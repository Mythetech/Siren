using BlazorMonaco.Editor;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Commands;
using Siren.Components.Http.Headers;
using Siren.Components.Http.Models;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Shared;
using Siren.Components.Variables;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class RequestPanelTests : TestContext
{
    private const string GetValue = "blazorMonaco.editor.getValue";
    private const string SetValue = "blazorMonaco.editor.setValue";

    private readonly RequestTabsState _tabs;
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private IConsumer<CommitRequestEdits>? _commitConsumer;

    public RequestPanelTests()
    {
        var variables = Substitute.For<IVariableService>();
        variables.GetVariables().Returns([]);
        variables.GetEnvironments().Returns([]);
        _bus.When(bus => bus.Subscribe(Arg.Any<IConsumer<CommitRequestEdits>>()))
            .Do(call => _commitConsumer = call.Arg<IConsumer<CommitRequestEdits>>());

        _tabs = this.AddRequestTabs();
        Services.AddSingleton(_bus);
        Services.AddSingleton(variables);
        Services.AddSingleton<VariableState>();
        Services.AddSingleton(Substitute.For<IJsApiService>());
        Services.AddSingleton<RequestAuthenticationState>();
        Services.AddLogging();
        this.AddReadyJsGuards();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    // Monaco tells BlazorMonaco about a change by calling this method from JavaScript
    private static Task RaiseEditorChangeAsync(IRenderedComponent<RequestPanel> cut)
    {
        var monaco = cut.FindComponent<StandaloneCodeEditor>();
        return cut.InvokeAsync(() => monaco.Instance.EventCallback("OnDidChangeModelContent", "{}"));
    }

    private static Task ShowHeadersViewAsync(IRenderedComponent<RequestPanel> cut)
    {
        var dropdown = cut.FindComponent<PanelViewDropdown<RequestPanelViewType>>();
        return cut.InvokeAsync(() => dropdown.Instance.ValueChanged.InvokeAsync(RequestPanelViewType.Headers));
    }

    private static IEnumerable<string?> HeaderRowKeys(IRenderedComponent<RequestPanel> cut)
        => cut.FindComponent<HeadersEditor>().Instance.Headers.Select(h => h.Key);

    private static string SendButtonText(IRenderedComponent<RequestPanel> cut)
        => cut.Find(".send-request-button").TextContent.Trim();

    [Fact(DisplayName = "Typing in the URL field writes nothing to the body editor")]
    public async Task TypingUrl_DoesNotWriteToBodyEditor()
    {
        _tabs.SetRawBody(_tabs.Active.Id, """{"name":"Ada"}""");
        var cut = RenderComponent<RequestPanel>();

        await cut.Find("input.tokenized-input").InputAsync(new ChangeEventArgs { Value = "https://api.example.com/users" });
        // Lets anything the input queued on the renderer finish before the editor is checked
        await cut.InvokeAsync(() => { });

        var request = _tabs.Active.Request.ShouldNotBeNull();
        request.RequestUri.ShouldBe("https://api.example.com/users");
        request.RawBody.ShouldBe("""{"name":"Ada"}""");
        JSInterop.Invocations[SetValue].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Typing in the body editor reaches the active tab")]
    public async Task TypingBody_ReachesActiveTab()
    {
        JSInterop.Setup<string>(GetValue, _ => true).SetResult("""{"name":"Ada"}""");
        var cut = RenderComponent<RequestPanel>();

        await RaiseEditorChangeAsync(cut);

        _tabs.Active.Request.ShouldNotBeNull().RawBody.ShouldBe("""{"name":"Ada"}""");
    }

    [Fact(DisplayName = "Text still being read when the tab changes is stored in the tab it was typed in")]
    public async Task TextReadAfterTabSwitch_StaysWithItsTab()
    {
        var first = _tabs.Active;
        var read = JSInterop.Setup<string>(GetValue, _ => true);
        var cut = RenderComponent<RequestPanel>();
        var change = RaiseEditorChangeAsync(cut);

        var second = _tabs.OpenNew();
        cut.WaitForAssertion(() => cut.FindComponent<SirenEditor>().Instance.DocumentId.ShouldBe(second.Id));
        read.SetResult("typed in the first tab");
        await change;

        first.Request.ShouldNotBeNull().RawBody.ShouldBe("typed in the first tab");
        second.Request.ShouldNotBeNull().RawBody.ShouldBe("");
    }

    [Fact(DisplayName = "CommitRequestEdits waits until text still being read has reached the tab")]
    public async Task CommitRequestEdits_WaitsForPendingText()
    {
        var read = JSInterop.Setup<string>(GetValue, _ => true);
        var cut = RenderComponent<RequestPanel>();
        var change = RaiseEditorChangeAsync(cut);

        var commit = _commitConsumer.ShouldNotBeNull().Consume(new CommitRequestEdits());
        commit.IsCompleted.ShouldBeFalse();
        read.SetResult("typed");
        await Task.WhenAll(change, commit);

        _tabs.Active.Request.ShouldNotBeNull().RawBody.ShouldBe("typed");
    }

    [Fact(DisplayName = "Header rows follow the active tab")]
    public async Task HeaderRows_FollowActiveTab()
    {
        var first = _tabs.Active;
        _tabs.SetHeaders(first.Id, [new("X-First", "1")]);
        var second = _tabs.OpenNew();
        _tabs.SetHeaders(second.Id, [new("X-Second", "2")]);
        _tabs.Activate(first.Id);
        var cut = RenderComponent<RequestPanel>();
        await ShowHeadersViewAsync(cut);
        HeaderRowKeys(cut).ShouldBe(["X-First"]);

        _tabs.Activate(second.Id);

        cut.WaitForAssertion(() => HeaderRowKeys(cut).ShouldBe(["X-Second"]));
    }

    [Fact(DisplayName = "The button shows Cancel only for a tab that is sending")]
    public void SendButton_ShowsCancelOnlyForSendingTab()
    {
        var sending = _tabs.Active;
        var cut = RenderComponent<RequestPanel>();
        SendButtonText(cut).ShouldBe("Send");

        _tabs.SetSending(sending.Id, true);
        cut.WaitForAssertion(() => SendButtonText(cut).ShouldBe("Cancel"));

        _tabs.OpenNew();
        cut.WaitForAssertion(() => SendButtonText(cut).ShouldBe("Send"));
    }

    [Fact(DisplayName = "The method dropdown follows the active tab")]
    public void MethodDropdown_FollowsActiveTab()
    {
        _tabs.SetMethod(_tabs.Active.Id, HttpMethod.Get);
        var cut = RenderComponent<RequestPanel>();
        cut.WaitForAssertion(() => cut.FindComponent<HttpRequestMethodDropdown>().Instance.SelectedMethod.ShouldBe("GET"));

        _tabs.Open(new SirenNetworkRequest { Request = new HttpRequest { Method = HttpMethod.Post } });

        cut.WaitForAssertion(() => cut.FindComponent<HttpRequestMethodDropdown>().Instance.SelectedMethod.ShouldBe("POST"));
    }
}
