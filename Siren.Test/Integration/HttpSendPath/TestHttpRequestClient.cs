using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Siren.Components.History;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Settings;
using Siren.Test.Infrastructure;

namespace Siren.Test.Integration.HttpSendPath;

internal static class TestHttpRequestClient
{
    /// <summary>
    /// Builds the real client with an empty cookie jar and no authentication, over default settings
    /// unless the test supplies its own.
    /// </summary>
    public static HttpRequestClient Create(SettingsState? settings = null)
    {
        return new HttpRequestClient(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IHistoryService>(),
            new NullLogger<HttpRequestClient>(),
            settings ?? TestState.Settings(),
            new CookieService(),
            new RequestAuthenticationState());
    }
}

/// <summary>
/// The real client over a resolver that never answers, as for an internal host name with the VPN down.
/// Like the system resolver on macOS and Linux, a lookup that has started cannot be interrupted.
/// </summary>
internal sealed class HangingLookupClient : HttpRequestClient
{
    private readonly TaskCompletionSource _lookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public HangingLookupClient()
        : base(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IHistoryService>(),
            new NullLogger<HttpRequestClient>(),
            TestState.Settings(),
            new CookieService(),
            new RequestAuthenticationState())
    {
    }

    public Task LookupStarted => _lookupStarted.Task;

    protected internal override Task<IPAddress[]> ResolveHostAsync(string host, CancellationToken ct)
    {
        _lookupStarted.TrySetResult();
        return new TaskCompletionSource<IPAddress[]>().Task;
    }
}
