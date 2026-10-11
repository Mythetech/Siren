using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Components.Services;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class ResponsePanelTests : TestContext
{
    private const string SetValue = "blazorMonaco.editor.setValue";

    private readonly RequestTabsState _tabs;

    public ResponsePanelTests()
    {
        _tabs = this.AddRequestTabs();
        Services.AddSingleton(Substitute.For<IJsApiService>());
        Services.AddLogging();
        this.AddReadyJsGuards();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private SirenNetworkRequest OpenWithResponse(string displayText) => _tabs.Open(new SirenNetworkRequest
    {
        Request = new HttpRequest { RequestUri = "https://api.example.com/users", Method = HttpMethod.Get },
        Response = new RequestResult { StatusCode = 200, HttpStatusCode = System.Net.HttpStatusCode.OK },
        DisplayText = displayText,
    });

    private static SirenEditor Editor(IRenderedComponent<ResponsePanel> cut) => cut.FindComponent<SirenEditor>().Instance;

    [Fact(DisplayName = "The response editor shows the active tab's response")]
    public void Editor_ShowsActiveTabResponse()
    {
        var tab = OpenWithResponse("""{"id":1}""");

        var cut = RenderComponent<ResponsePanel>();

        Editor(cut).DocumentId.ShouldBe(tab.Id);
        Editor(cut).Text.ShouldBe("""{"id":1}""");
    }

    [Fact(DisplayName = "Switching tabs shows the other tab's response")]
    public void SwitchingTabs_ShowsOtherResponse()
    {
        var first = OpenWithResponse("""{"id":1}""");
        OpenWithResponse("""{"id":2}""");
        var cut = RenderComponent<ResponsePanel>();

        _tabs.Activate(first.Id);

        cut.WaitForAssertion(() => Editor(cut).Text.ShouldBe("""{"id":1}"""));
    }

    [Fact(DisplayName = "A new response for the active tab is shown")]
    public void NewResponse_IsShown()
    {
        var cut = RenderComponent<ResponsePanel>();

        _tabs.SetResponse(_tabs.Active.Id, new RequestResult { StatusCode = 200 }, """{"id":3}""");

        cut.WaitForAssertion(() => Editor(cut).Text.ShouldBe("""{"id":3}"""));
    }

    [Fact(DisplayName = "Editing the request does not rewrite the response editor")]
    public void EditingRequest_DoesNotRewriteResponseEditor()
    {
        var tab = OpenWithResponse("""{"id":1}""");
        var cut = RenderComponent<ResponsePanel>();

        _tabs.SetUri(tab.Id, "https://api.example.com/changed");
        cut.Render();

        JSInterop.Invocations[SetValue].ShouldBeEmpty();
    }

    [Fact(DisplayName = "A response for a tab that is not active does not change the panel")]
    public void ResponseForOtherTab_DoesNotChangePanel()
    {
        var other = OpenWithResponse("""{"id":1}""");
        var active = OpenWithResponse("""{"id":2}""");
        var cut = RenderComponent<ResponsePanel>();

        _tabs.SetResponse(other.Id, new RequestResult { StatusCode = 500 }, "other tab's new response");
        cut.Render();

        Editor(cut).DocumentId.ShouldBe(active.Id);
        Editor(cut).Text.ShouldBe("""{"id":2}""");
        JSInterop.Invocations[SetValue].ShouldBeEmpty();
    }
}
