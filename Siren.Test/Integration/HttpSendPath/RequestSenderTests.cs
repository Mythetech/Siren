using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Siren.Components;
using Siren.Components.Http;
using Siren.Components.Http.Commands;
using Siren.Components.Http.Models;
using Siren.Components.Services;
using Siren.Components.SessionStats;
using Siren.Components.Settings;
using Siren.Components.Variables;
using Siren.Test.Infrastructure;

namespace Siren.Test.Integration.HttpSendPath;

public class RequestSenderTests : IClassFixture<EchoHostFixture>
{
    private const string Dev = "Dev";

    // A send that is still running after this long counts as hung, and the test fails
    private static readonly TimeSpan TestLimit = TimeSpan.FromSeconds(5);

    private readonly EchoHostFixture _host;
    private readonly SettingsState _settings = TestState.Settings();
    private readonly RequestTabsState _tabs;
    private readonly SirenAppState _appState;
    private readonly SessionStatsState _stats = new();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly List<Variable> _variables = [];
    private readonly IRequestComposer _composer;
    private readonly RequestSender _sender;

    public RequestSenderTests(EchoHostFixture host)
    {
        _host = host;
        _tabs = TestState.Tabs(_settings);
        _appState = TestState.AppState(_tabs);

        var variableService = Substitute.For<IVariableService>();
        variableService.GetVariables().Returns(_ => _variables.ToList());
        _composer = new RequestComposer(new VariableSubstitutionService(variableService, [new FailingVariableResolver()]));

        _sender = CreateSender();
    }

    private string EchoUrl => $"{_host.BaseUrl}/echo";

    private RequestSender CreateSender(IRequestComposer? composer = null, IHttpRequestClient? client = null) => new(
        _tabs,
        composer ?? _composer,
        client ?? TestHttpRequestClient.Create(_settings),
        _settings,
        _stats,
        _appState,
        _bus,
        NullLogger<RequestSender>.Instance);

    private SirenNetworkRequest OpenTab(string url, HttpMethod? method = null, string body = "")
    {
        return _tabs.Open(new SirenNetworkRequest
        {
            Request = new HttpRequest
            {
                RequestUri = url,
                Method = method ?? HttpMethod.Get,
                BodyType = body == "" ? RequestBodyType.None : RequestBodyType.Raw,
                RawBody = body,
            }
        });
    }

    private Task<SendOutcome> SendAsync(SirenNetworkRequest tab) => _sender.SendAsync(tab.Id).WaitAsync(TestLimit);

    private Task ShouldHaveNotifiedAsync(string message, Severity severity)
        => _bus.Received(1).PublishAsync(new AddNotification(message, severity));

    private Task ShouldNotHaveNotifiedAsync()
        => _bus.DidNotReceive().PublishAsync(Arg.Any<AddNotification>());

    [Fact(DisplayName = "A response lands in the tab that sent it when another tab is active")]
    public async Task Response_LandsInSendingTab()
    {
        var held = _host.Hold();
        var sending = OpenTab(held.Url);
        var other = OpenTab(EchoUrl);

        var send = SendAsync(sending);
        await held.Arrived.WaitAsync(TestLimit);
        _tabs.Active.ShouldBeSameAs(other);
        held.Release();

        (await send).ShouldBe(SendOutcome.Completed);
        sending.Response.ShouldNotBeNull().StatusCode.ShouldBe(200);
        other.Response.ShouldBeNull();
    }

    [Fact(DisplayName = "Only the sending tab is marked as sending, and only while it sends")]
    public async Task IsSending_TracksTheSendingTab()
    {
        var held = _host.Hold();
        var sending = OpenTab(held.Url);
        var other = OpenTab(EchoUrl);

        var send = SendAsync(sending);
        await held.Arrived.WaitAsync(TestLimit);
        sending.IsSending.ShouldBeTrue();
        other.IsSending.ShouldBeFalse();
        held.Release();
        await send;

        sending.IsSending.ShouldBeFalse();
    }

    [Fact(DisplayName = "Two tabs can send at the same time")]
    public async Task TwoTabs_SendAtTheSameTime()
    {
        var firstHeld = _host.Hold();
        var secondHeld = _host.Hold();
        var first = OpenTab(firstHeld.Url);
        var second = OpenTab(secondHeld.Url);

        var firstSend = SendAsync(first);
        var secondSend = SendAsync(second);
        await Task.WhenAll(firstHeld.Arrived, secondHeld.Arrived).WaitAsync(TestLimit);
        secondHeld.Release();
        firstHeld.Release();

        (await firstSend).ShouldBe(SendOutcome.Completed);
        (await secondSend).ShouldBe(SendOutcome.Completed);
    }

    [Fact(DisplayName = "A second send on a tab that is sending is refused")]
    public async Task SecondSend_IsRefused()
    {
        var held = _host.Hold();
        var tab = OpenTab(held.Url);
        var first = SendAsync(tab);
        await held.Arrived.WaitAsync(TestLimit);

        (await SendAsync(tab)).ShouldBe(SendOutcome.AlreadySending);

        held.Release();
        (await first).ShouldBe(SendOutcome.Completed);
        held.Arrivals.ShouldBe(1);
    }

    [Fact(DisplayName = "Cancelling one tab cancels only that tab and says so neutrally")]
    public async Task Cancel_AffectsOnlyItsTab()
    {
        var cancelledHeld = _host.Hold();
        var otherHeld = _host.Hold();
        var cancelled = OpenTab(cancelledHeld.Url);
        var other = OpenTab(otherHeld.Url);
        var cancelledSend = SendAsync(cancelled);
        var otherSend = SendAsync(other);
        await Task.WhenAll(cancelledHeld.Arrived, otherHeld.Arrived).WaitAsync(TestLimit);

        _sender.Cancel(cancelled.Id);

        (await cancelledSend).ShouldBe(SendOutcome.Cancelled);
        cancelled.Response.ShouldBeNull();
        cancelled.IsSending.ShouldBeFalse();
        other.IsSending.ShouldBeTrue();
        otherHeld.Release();
        (await otherSend).ShouldBe(SendOutcome.Completed);
        await ShouldHaveNotifiedAsync("Request cancelled", Severity.Info);
    }

    [Fact(DisplayName = "Cancelling a tab that is not sending does nothing")]
    public void Cancel_NotSending_DoesNothing()
    {
        Should.NotThrow(() => _sender.Cancel(_tabs.Active.Id));
        Should.NotThrow(() => _sender.Cancel(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Closing a tab while it sends cancels the request and drops the result")]
    public async Task CloseWhileSending_DropsResult()
    {
        var held = _host.Hold();
        var tab = OpenTab(held.Url);
        var send = SendAsync(tab);
        await held.Arrived.WaitAsync(TestLimit);

        _tabs.Close(tab.Id);

        (await send).ShouldBe(SendOutcome.TabClosed);
        tab.Response.ShouldBeNull();
        await ShouldNotHaveNotifiedAsync();
    }

    [Fact(DisplayName = "Sending a tab that is not open does nothing")]
    public async Task Send_UnknownTab_ReturnsTabClosed()
    {
        (await _sender.SendAsync(Guid.NewGuid())).ShouldBe(SendOutcome.TabClosed);
    }

    [Fact(DisplayName = "The Request Timeout setting ends a slow request as timed out")]
    public async Task RequestTimeoutSetting_TimesOut()
    {
        _settings.RequestTimeout = 1;
        var held = _host.Hold();
        var tab = OpenTab(held.Url);
        try
        {
            (await SendAsync(tab)).ShouldBe(SendOutcome.TimedOut);
        }
        finally
        {
            held.Release();
        }

        tab.Response.ShouldBeNull();
        tab.IsSending.ShouldBeFalse();
        await ShouldHaveNotifiedAsync("The request timed out after 1 second", Severity.Error);
    }

    [Fact(DisplayName = "An unreachable host fails without throwing, and the tab stops sending")]
    public async Task UnreachableHost_Fails()
    {
        var tab = OpenTab("http://127.0.0.1:1/");

        (await SendAsync(tab)).ShouldBe(SendOutcome.Failed);

        tab.IsSending.ShouldBeFalse();
        tab.Response.ShouldBeNull();
        await ShouldHaveNotifiedAsync("Invalid URL or hostname could not be resolved", Severity.Error);
    }

    [Fact(DisplayName = "A URL typed as host and port is reported as an invalid URL")]
    public async Task HostAndPortWithoutScheme_IsInvalidUrl()
    {
        var tab = OpenTab("localhost:8000/x");

        (await SendAsync(tab)).ShouldBe(SendOutcome.Failed);

        await ShouldHaveNotifiedAsync("Invalid URL", Severity.Error);
    }

    [Fact(DisplayName = "A URL that cannot be parsed is reported as an invalid URL")]
    public async Task UnparseableUrl_IsInvalidUrl()
    {
        var tab = OpenTab("http://");

        (await SendAsync(tab)).ShouldBe(SendOutcome.Failed);

        await ShouldHaveNotifiedAsync("Invalid URL", Severity.Error);
    }

    [Fact(DisplayName = "A request that cannot be built fails without throwing or showing the error text")]
    public async Task ComposerThrows_Fails()
    {
        var composer = Substitute.For<IRequestComposer>();
        composer.ComposeAsync(Arg.Any<HttpRequest>(), Arg.Any<string?>())
            .ThrowsAsync(new InvalidOperationException("internal detail"));
        var tab = OpenTab(EchoUrl);

        (await CreateSender(composer).SendAsync(tab.Id)).ShouldBe(SendOutcome.Failed);

        tab.IsSending.ShouldBeFalse();
        await ShouldHaveNotifiedAsync("The request failed", Severity.Error);
    }

    [Fact(DisplayName = "A blank URL is reported and nothing is sent")]
    public async Task BlankUrl_IsRejected()
    {
        var client = Substitute.For<IHttpRequestClient>();
        var tab = OpenTab("   ");

        (await CreateSender(client: client).SendAsync(tab.Id)).ShouldBe(SendOutcome.MissingUrl);

        await client.DidNotReceiveWithAnyArgs().SendHttpRequestAsync(default!, default);
        tab.IsSending.ShouldBeFalse();
        await ShouldHaveNotifiedAsync("Request URL Required", Severity.Error);
    }

    [Fact(DisplayName = "A missing method is reported and nothing is sent")]
    public async Task MissingMethod_IsRejected()
    {
        var client = Substitute.For<IHttpRequestClient>();
        var tab = _tabs.Open(new SirenNetworkRequest { Request = new HttpRequest { RequestUri = EchoUrl } });

        (await CreateSender(client: client).SendAsync(tab.Id)).ShouldBe(SendOutcome.MissingMethod);

        await client.DidNotReceiveWithAnyArgs().SendHttpRequestAsync(default!, default);
        await ShouldHaveNotifiedAsync("HTTP Method Required", Severity.Error);
    }

    [Fact(DisplayName = "A URL that resolves to blank is reported as missing and the tab stops sending")]
    public async Task UrlResolvingToBlank_IsRejected()
    {
        _variables.Add(Variable.Create("base", "", group: Dev));
        _appState.ActiveEnvironment = Dev;
        var tab = OpenTab("{{base}}");

        (await SendAsync(tab)).ShouldBe(SendOutcome.MissingUrl);

        tab.IsSending.ShouldBeFalse();
        await ShouldHaveNotifiedAsync("Request URL Required", Severity.Error);
    }

    [Fact(DisplayName = "A variable that cannot be resolved is reported and the request is still sent")]
    public async Task ResolutionError_WarnsAndSends()
    {
        _variables.Add(Variable.Create("token", FailingVariableResolver.Value, group: Dev));
        _appState.ActiveEnvironment = Dev;
        var tab = _tabs.Open(new SirenNetworkRequest
        {
            Request = new HttpRequest
            {
                RequestUri = EchoUrl,
                Method = HttpMethod.Get,
                BodyType = RequestBodyType.None,
                Headers = [new("X-Token", "{{token}}")],
            }
        });

        (await SendAsync(tab)).ShouldBe(SendOutcome.Completed);

        await ShouldHaveNotifiedAsync("Warning: 1 variable(s) could not be fully resolved", Severity.Warning);
    }

    [Fact(DisplayName = "Text committed for CommitRequestEdits is part of the request that is sent")]
    public async Task CommittedEdits_AreSent()
    {
        var tab = OpenTab(EchoUrl, HttpMethod.Post, body: "{}");
        _bus.When(bus => bus.PublishAsync(Arg.Any<CommitRequestEdits>()))
            .Do(_ => _tabs.SetRawBody(tab.Id, """{"typed":"late"}"""));

        (await SendAsync(tab)).ShouldBe(SendOutcome.Completed);

        var echo = JsonDocument.Parse(tab.Response.ShouldNotBeNull().ResponseText).RootElement.Clone();
        echo.GetProperty("body").GetString().ShouldBe("""{"typed":"late"}""");
    }

    [Fact(DisplayName = "A completed request is recorded in the session stats and raises no notification")]
    public async Task Completed_IsRecordedQuietly()
    {
        (await SendAsync(OpenTab(EchoUrl))).ShouldBe(SendOutcome.Completed);

        _stats.TotalRequests.ShouldBe(1);
        await ShouldNotHaveNotifiedAsync();
    }

    [Fact(DisplayName = "A JSON response is indented for display")]
    public async Task JsonResponse_IsIndented()
    {
        var tab = OpenTab(EchoUrl);

        await SendAsync(tab);

        tab.DisplayText.ShouldNotBeNull().ShouldStartWith("{" + System.Environment.NewLine);
        tab.Response.ShouldNotBeNull().ResponseText.ShouldNotContain(System.Environment.NewLine);
    }

    [Fact(DisplayName = "A response that is not JSON is shown as received")]
    public async Task NonJsonResponse_IsShownAsReceived()
    {
        var tab = OpenTab($"{_host.BaseUrl}/plain");

        (await SendAsync(tab)).ShouldBe(SendOutcome.Completed);

        tab.DisplayText.ShouldBe("plain text, not JSON");
    }

    [Fact(DisplayName = "An empty response is shown as empty")]
    public async Task EmptyResponse_IsShownAsEmpty()
    {
        var tab = OpenTab(EchoUrl, HttpMethod.Head);

        (await SendAsync(tab)).ShouldBe(SendOutcome.Completed);

        tab.DisplayText.ShouldBe("");
    }
}
