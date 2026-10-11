using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Components.RequestContextPanel;
using Siren.Components.Services;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class HeadersPanelTests : TestContext
{
    private readonly RequestTabsState _tabs;

    public HeadersPanelTests()
    {
        _tabs = this.AddRequestTabs();
        Services.AddSingleton(Substitute.For<IJsApiService>());
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private static IEnumerable<string?> RowKeys(IRenderedComponent<HeadersPanel> cut)
        => cut.FindComponent<MudDataGrid<HeaderModel>>().Instance.Items.Select(h => h.Key);

    [Fact(DisplayName = "The panel shows the active tab's header rows")]
    public void Panel_ShowsActiveTabHeaders()
    {
        _tabs.SetHeaders(_tabs.Active.Id, [new("X-First", "1")]);

        var cut = RenderComponent<HeadersPanel>();

        RowKeys(cut).ShouldBe(["X-First"]);
    }

    [Fact(DisplayName = "Header rows do not carry over to another tab")]
    public void HeaderRows_DoNotCarryOver()
    {
        var first = _tabs.Active;
        var cut = RenderComponent<HeadersPanel>();
        _tabs.SetHeaders(first.Id, [new("X-First", "1")]);
        cut.WaitForAssertion(() => RowKeys(cut).ShouldBe(["X-First"]));

        var second = _tabs.OpenNew();
        _tabs.SetHeaders(second.Id, [new("X-Second", "2")]);

        cut.WaitForAssertion(() => RowKeys(cut).ShouldBe(["X-Second"]));
    }

    [Fact(DisplayName = "Adding a header row stores it on the tab through the state")]
    public async Task AddHeader_StoresOnTab()
    {
        var changes = 0;
        _tabs.RequestChanged += _ => changes++;
        var cut = RenderComponent<HeadersPanel>();

        var add = cut.FindComponents<SirenIconButton>().Single(b => b.Instance.Tooltip == "Add Header");
        await add.Find("button").ClickAsync(new MouseEventArgs());

        _tabs.Active.Request.ShouldNotBeNull().Headers.ShouldBe([new KeyValuePair<string, string>("Key", "Value")]);
        changes.ShouldBe(1);
    }

    [Fact(DisplayName = "Two header rows with the same name do not break the panel")]
    public async Task DuplicateHeaderNames_DoNotThrow()
    {
        var cut = RenderComponent<HeadersPanel>();

        for (var i = 0; i < 2; i++)
        {
            var add = cut.FindComponents<SirenIconButton>().Single(b => b.Instance.Tooltip == "Add Header");
            await add.Find("button").ClickAsync(new MouseEventArgs());
        }

        cut.WaitForAssertion(() => RowKeys(cut).ShouldBe(["Key", "Key"]));
    }
}
