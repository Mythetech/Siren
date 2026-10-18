using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
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
        => (cut.FindComponent<MudDataGrid<HeaderModel>>().Instance.Items ?? []).Select(h => h.Key);

    private static IEnumerable<string?> RowValues(IRenderedComponent<HeadersPanel> cut)
        => (cut.FindComponent<MudDataGrid<HeaderModel>>().Instance.Items ?? []).Select(h => h.Value);

    /// <summary>
    /// The text boxes of the header rows in reading order: each row's name, then its value.
    /// </summary>
    private static IReadOnlyList<IElement> CellInputs(IRenderedComponent<HeadersPanel> cut)
        => cut.FindAll("input").Where(input => input.GetAttribute("type") == "text").ToList();

    private static List<HeaderModel> Rows(IRenderedComponent<HeadersPanel> cut)
        => (cut.FindComponent<MudDataGrid<HeaderModel>>().Instance.Items ?? []).ToList();

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

    [Fact(DisplayName = "Editing a header value in the panel stores the new value on the tab")]
    public async Task EditingValue_StoresOnTab()
    {
        _tabs.SetHeaders(_tabs.Active.Id, [new("X-A", "1")]);
        var cut = RenderComponent<HeadersPanel>();

        // The browser reports a cell edit as a change, then a blur when focus leaves the cell
        await CellInputs(cut)[1].ChangeAsync(new ChangeEventArgs { Value = "2" });
        await CellInputs(cut)[1].BlurAsync(new FocusEventArgs());

        _tabs.Active.Request.ShouldNotBeNull().Headers.ShouldBe([new KeyValuePair<string, string>("X-A", "2")]);
        RowValues(cut).ShouldBe(["2"]);
    }

    [Fact(DisplayName = "Renaming a header in the panel replaces the old name on the tab")]
    public async Task RenamingHeader_ReplacesOldName()
    {
        _tabs.SetHeaders(_tabs.Active.Id, [new("X-Old", "1")]);
        var cut = RenderComponent<HeadersPanel>();

        await CellInputs(cut)[0].ChangeAsync(new ChangeEventArgs { Value = "X-New" });
        await CellInputs(cut).Single(input => input.GetAttribute("value") == "X-New").BlurAsync(new FocusEventArgs());

        _tabs.Active.Request.ShouldNotBeNull().Headers.ShouldBe([new KeyValuePair<string, string>("X-New", "1")]);
        RowKeys(cut).ShouldBe(["X-New"]);
    }

    [Fact(DisplayName = "Switching a header off keeps its row where it is")]
    public async Task SwitchingOff_KeepsRowInPlace()
    {
        _tabs.SetHeaders(_tabs.Active.Id, [new("X-First", "1"), new("X-Second", "2")]);
        var cut = RenderComponent<HeadersPanel>();

        var firstSwitch = cut.FindComponents<SirenSwitch<bool>>()[0];
        await cut.InvokeAsync(() => firstSwitch.Instance.ValueChanged.InvokeAsync(false));

        _tabs.Active.Request.ShouldNotBeNull().Headers.ShouldBe([new KeyValuePair<string, string>("X-Second", "2")]);
        cut.WaitForAssertion(() => RowKeys(cut).ShouldBe(["X-First", "X-Second"]));
    }

    [Fact(DisplayName = "Headers changed elsewhere show up in the panel")]
    public void HeadersChangedElsewhere_ShowUp()
    {
        _tabs.SetHeaders(_tabs.Active.Id, [new("X-A", "1")]);
        var cut = RenderComponent<HeadersPanel>();

        _tabs.SetHeaders(_tabs.Active.Id, [new("X-A", "9"), new("X-B", "2")]);

        cut.WaitForAssertion(() => RowKeys(cut).ShouldBe(["X-A", "X-B"]));
        RowValues(cut).ShouldBe(["9", "2"]);
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

    [Fact(DisplayName = "A header renamed in the other editor is not kept or sent again")]
    public async Task HeaderRenamedElsewhere_IsNotKept()
    {
        _tabs.SetHeaders(_tabs.Active.Id, [new("X-Old", "1")]);
        var cut = RenderComponent<HeadersPanel>();

        _tabs.SetHeaders(_tabs.Active.Id, [new("X-New", "1")]);
        cut.WaitForAssertion(() => RowKeys(cut).ShouldBe(["X-New"]));

        await CellInputs(cut)[1].ChangeAsync(new ChangeEventArgs { Value = "2" });

        _tabs.Active.Request.ShouldNotBeNull().Headers.ShouldBe([new KeyValuePair<string, string>("X-New", "2")]);
    }

    [Fact(DisplayName = "Two rows with one name keep their rows when the headers change elsewhere")]
    public void RepeatedNames_KeepRows()
    {
        _tabs.SetHeaders(_tabs.Active.Id, [new("X", "1"), new("X", "2")]);
        var cut = RenderComponent<HeadersPanel>();
        var rows = Rows(cut);

        _tabs.SetHeaders(_tabs.Active.Id, [new("X", "3"), new("X", "4")]);

        cut.WaitForAssertion(() => RowValues(cut).ShouldBe(["3", "4"]));
        Rows(cut).ShouldBe(rows);
    }
}
