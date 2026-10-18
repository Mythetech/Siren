using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using Siren.Components.Http.Commands;
using Siren.Components.Http.Models;
using Siren.Components.Services;
using Siren.Components.SessionStats;
using Siren.Components.Settings;

namespace Siren.Components.Http;

public enum SendOutcome
{
    Completed,
    MissingUrl,
    MissingMethod,
    AlreadySending,
    Failed,
    TimedOut,
    Cancelled,
    TabClosed,
}

public interface IRequestSender
{
    /// <summary>
    /// Sends the request in the given tab and stores the response on that tab. Never throws:
    /// every way a send can go wrong comes back as an outcome.
    /// </summary>
    Task<SendOutcome> SendAsync(Guid tabId);

    /// <summary>
    /// Cancels the request the given tab is sending, if it is sending one.
    /// </summary>
    void Cancel(Guid tabId);
}

public class RequestSender : IRequestSender
{
    private const string UrlRequired = "Request URL Required";
    private const string MethodRequired = "HTTP Method Required";
    private const string RequestFailed = "The request failed";

    private static readonly JsonSerializerOptions DisplayJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly RequestTabsState _tabs;
    private readonly IRequestComposer _composer;
    private readonly IHttpRequestClient _client;
    private readonly SettingsState _settings;
    private readonly SessionStatsState _sessionStats;
    private readonly SirenAppState _appState;
    private readonly IMessageBus _bus;
    private readonly ILogger<RequestSender> _logger;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sending = new();

    public RequestSender(
        RequestTabsState tabs,
        IRequestComposer composer,
        IHttpRequestClient client,
        SettingsState settings,
        SessionStatsState sessionStats,
        SirenAppState appState,
        IMessageBus bus,
        ILogger<RequestSender> logger)
    {
        _tabs = tabs;
        _composer = composer;
        _client = client;
        _settings = settings;
        _sessionStats = sessionStats;
        _appState = appState;
        _bus = bus;
        _logger = logger;

        _tabs.TabsChanged += CancelClosedTabs;
    }

    public async Task<SendOutcome> SendAsync(Guid tabId)
    {
        try
        {
            // Text the user has just typed may still be on its way from the editor
            await _bus.PublishAsync(new CommitRequestEdits());

            var tab = Find(tabId);
            if (tab?.Request == null)
                return SendOutcome.TabClosed;

            if (tab.IsSending)
                return SendOutcome.AlreadySending;

            if (string.IsNullOrWhiteSpace(tab.Request.RequestUri))
                return await RejectAsync(SendOutcome.MissingUrl, UrlRequired);

            if (string.IsNullOrWhiteSpace(tab.Request.Method?.Method))
                return await RejectAsync(SendOutcome.MissingMethod, MethodRequired);

            return await SendTabAsync(tab.Id, tab.Request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending the request in tab {TabId}", tabId);
            await NotifyFailureAsync();
            return SendOutcome.Failed;
        }
    }

    public void Cancel(Guid tabId)
    {
        if (_sending.TryGetValue(tabId, out var cancellation))
            Stop(cancellation);
    }

    private async Task<SendOutcome> SendTabAsync(Guid tabId, HttpRequest template)
    {
        using var cancellation = new CancellationTokenSource();
        string? requestUri = null;

        try
        {
            // Inside the try, so the finally clears both even when a SendingChanged subscriber throws
            _sending[tabId] = cancellation;
            _tabs.SetSending(tabId, true);

            var composed = await _composer.ComposeAsync(template, _appState.ActiveEnvironment);

            if (composed.ResolutionErrors.Count > 0)
            {
                _logger.LogWarning("Variable resolution errors: {Errors}", string.Join("; ", composed.ResolutionErrors));
                await NotifyAsync($"Warning: {composed.ResolutionErrors.Count} variable(s) could not be fully resolved", Severity.Warning);
            }

            var request = composed.Request;
            requestUri = request.RequestUri;

            if (string.IsNullOrWhiteSpace(requestUri))
                return await RejectAsync(SendOutcome.MissingUrl, UrlRequired);

            request.Timeout = TimeSpan.FromSeconds(_settings.RequestTimeout);

            _logger.LogInformation("Sending HTTP request to {RequestUri}", requestUri);
            var result = await _client.SendHttpRequestAsync(request, cancellation.Token);

            if (result.Error != null)
                return await FailAsync(tabId, result.Error, requestUri, cancellation);

            if (!_tabs.SetResponse(tabId, result, ToDisplayText(result.ResponseText)))
                return SendOutcome.TabClosed;

            RecordStats(requestUri, result);
            return SendOutcome.Completed;
        }
        catch (Exception ex)
        {
            return await FailAsync(tabId, ex, requestUri, cancellation);
        }
        finally
        {
            _sending.TryRemove(tabId, out _);
            _tabs.SetSending(tabId, false);
        }
    }

    private async Task<SendOutcome> FailAsync(Guid tabId, Exception error, string? requestUri, CancellationTokenSource cancellation)
    {
        if (Find(tabId) == null)
            return SendOutcome.TabClosed;

        if (cancellation.IsCancellationRequested)
        {
            await NotifyAsync("Request cancelled", Severity.Info);
            return SendOutcome.Cancelled;
        }

        if (error is TimeoutException)
        {
            var seconds = _settings.RequestTimeout;
            _logger.LogWarning("Request to {RequestUri} timed out after {Seconds} seconds", requestUri, seconds);
            await NotifyAsync($"The request timed out after {seconds} {(seconds == 1 ? "second" : "seconds")}", Severity.Error);
            return SendOutcome.TimedOut;
        }

        _logger.LogError(error, "Request to {RequestUri} failed", requestUri);
        await NotifyAsync(FailureMessage(error, requestUri), Severity.Error);
        return SendOutcome.Failed;
    }

    private static string FailureMessage(Exception error, string? requestUri)
    {
        if (error is UriFormatException || HasUnsupportedScheme(requestUri))
            return "Invalid URL";

        if (error is HttpRequestException or SocketException)
            return "Invalid URL or hostname could not be resolved";

        return RequestFailed;
    }

    // A URL typed as host:port, such as localhost:8000/x, parses as an absolute URL whose scheme is the host
    private static bool HasUnsupportedScheme(string? requestUri)
        => Uri.TryCreate(requestUri?.Trim(), UriKind.Absolute, out var uri)
           && uri.Scheme != Uri.UriSchemeHttp
           && uri.Scheme != Uri.UriSchemeHttps;

    private static string ToDisplayText(string? responseText)
    {
        var text = responseText ?? "";

        try
        {
            using var document = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(document, DisplayJsonOptions);
        }
        catch (JsonException)
        {
            // Not JSON, so it is shown as received
            return text;
        }
    }

    private void CancelClosedTabs()
    {
        var open = _tabs.Tabs.Select(t => t.Id).ToHashSet();

        foreach (var (tabId, cancellation) in _sending)
        {
            if (!open.Contains(tabId))
                Stop(cancellation);
        }
    }

    private static void Stop(CancellationTokenSource cancellation)
    {
        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The send finished between the lookup and the cancel, so there is nothing left to stop
        }
    }

    private SirenNetworkRequest? Find(Guid tabId) => _tabs.Tabs.FirstOrDefault(t => t.Id == tabId);

    private async Task<SendOutcome> RejectAsync(SendOutcome outcome, string message)
    {
        await NotifyAsync(message, Severity.Error);
        return outcome;
    }

    // The response is on screen by now, so a failure here is logged and the send still counts as completed
    private void RecordStats(string requestUri, RequestResult result)
    {
        try
        {
            _sessionStats.RecordRequest(requestUri, result.StatusCode, result.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not record the session stats for {RequestUri}", requestUri);
        }
    }

    // Called from the last catch of SendAsync, which promises never to throw, so a toast that fails is only logged
    private async Task NotifyFailureAsync()
    {
        try
        {
            await NotifyAsync(RequestFailed, Severity.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not show the failure notification");
        }
    }

    private Task NotifyAsync(string message, Severity severity)
        => _bus.PublishAsync(new AddNotification(message, severity));
}
