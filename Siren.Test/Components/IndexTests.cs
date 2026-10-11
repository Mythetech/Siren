using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Mythetech.Framework.Desktop.Updates;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Smoke;
using NSubstitute;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Variables;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class IndexTests : TestContext
{
    // bUnit records ElementReference.FocusAsync under the name Blazor gives the call
    private const string Focus = "Blazor._internal.domWrapper.focus";

    private readonly RequestTabsState _tabs;
    private readonly IRequestSender _sender = Substitute.For<IRequestSender>();

    public IndexTests()
    {
        var variables = Substitute.For<IVariableService>();
        variables.GetVariables().Returns([]);
        variables.GetEnvironments().Returns([]);
        var smokeTest = Substitute.For<ISmokeTestContext>();
        smokeTest.IsEnabled.Returns(true);

        _tabs = this.AddRequestTabs();
        Services.AddSingleton(_sender);
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(variables);
        Services.AddSingleton<VariableState>();
        Services.AddSingleton(Substitute.For<IJsApiService>());
        Services.AddSingleton(Substitute.For<IFileOpenService>());
        Services.AddSingleton<RequestAuthenticationState>();
        Services.AddSingleton<PluginState>();
        Services.AddSingleton<PluginLoader>();
        Services.AddSingleton(Substitute.For<IUpdateService>());
        Services.AddSingleton(smokeTest);
        Services.AddLogging();
        this.AddReadyJsGuards();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private int FocusCalls => JSInterop.Invocations[Focus].Count;

    /// <summary>
    /// Clicks Send for a request that only ends when the returned source is completed.
    /// </summary>
    private (Task Click, TaskCompletionSource<SendOutcome> Send) ClickSend(IRenderedComponent<Siren.Components.Index> cut)
    {
        var send = new TaskCompletionSource<SendOutcome>();
        _sender.SendAsync(_tabs.Active.Id).Returns(send.Task);

        return (cut.Find(".send-request-button").ClickAsync(new MouseEventArgs()), send);
    }

    [Theory(DisplayName = "A send that ends badly on the tab still on screen moves focus to a field to correct")]
    [InlineData(SendOutcome.MissingUrl)]
    [InlineData(SendOutcome.MissingMethod)]
    [InlineData(SendOutcome.Failed)]
    [InlineData(SendOutcome.TimedOut)]
    public async Task BadOutcomeOnVisibleTab_MovesFocus(SendOutcome outcome)
    {
        var cut = RenderComponent<Siren.Components.Index>();
        var before = FocusCalls;
        var (click, send) = ClickSend(cut);

        send.SetResult(outcome);
        await click;

        FocusCalls.ShouldBe(before + 1);
    }

    [Fact(DisplayName = "Clicking the button on a tab that is sending cancels that tab's request")]
    public async Task Click_WhileSending_CancelsThatTab()
    {
        var cut = RenderComponent<Siren.Components.Index>();
        var tab = _tabs.Active;
        _tabs.SetSending(tab.Id, true);
        cut.WaitForAssertion(() => cut.Find(".send-request-button").TextContent.Trim().ShouldBe("Cancel"));

        await cut.Find(".send-request-button").ClickAsync(new MouseEventArgs());

        _sender.Received(1).Cancel(tab.Id);
        await _sender.DidNotReceive().SendAsync(Arg.Any<Guid>());
    }

    [Theory(DisplayName = "A send that ends badly after the user moved to another tab leaves focus where it is")]
    [InlineData(SendOutcome.Failed)]
    [InlineData(SendOutcome.TimedOut)]
    public async Task BadOutcomeOnAnotherTab_LeavesFocusAlone(SendOutcome outcome)
    {
        var cut = RenderComponent<Siren.Components.Index>();
        var (click, send) = ClickSend(cut);
        _tabs.OpenNew();
        var before = FocusCalls;

        send.SetResult(outcome);
        await click;

        FocusCalls.ShouldBe(before);
    }
}
