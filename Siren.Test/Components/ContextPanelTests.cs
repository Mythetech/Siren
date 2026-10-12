using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;
using Siren.Components.AppContextPanel;
using Siren.Components.History;
using Siren.Components.SessionStats;
using Siren.Test.Infrastructure;
using RailItem = Mythetech.Framework.Components.AppContextDrawer.ContextPanelItem;

namespace Siren.Test.Components;

public class ContextPanelTests : TestContext
{
    private readonly SessionStatsSettings _sessionStats = new();

    public ContextPanelTests()
    {
        Services.AddLogging();
        Services.AddSingleton(TestState.AppState());
        Services.AddSingleton(TestState.Settings());
        Services.AddSingleton(_sessionStats);
        Services.AddSingleton(new PluginState());
        Services.AddSingleton(Substitute.For<IMessageBus>());
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();

        // These tests are about the rail; the History panel itself has tests of its own
        ComponentFactories.AddStub<HistoryPanel>();
    }

    private static List<string> PanelIds(IRenderedComponent<ContextPanel> cut)
        => cut.FindComponents<RailItem>().Select(item => item.Instance.Id).ToList();

    [Fact(DisplayName = "The rail holds Siren's panels in their order")]
    public void Rail_HoldsSirensPanelsInOrder()
    {
        var cut = RenderComponent<ContextPanel>();

        PanelIds(cut).ShouldBe([
            SirenPanels.History, SirenPanels.Collections, SirenPanels.Variables, SirenPanels.MockServer, SirenPanels.SessionStats,
        ]);
    }

    [Fact(DisplayName = "The session stats panel is left out when its setting is off")]
    public void SessionStatsOff_LeavesItsPanelOut()
    {
        _sessionStats.ShowSessionStats = false;

        var cut = RenderComponent<ContextPanel>();

        PanelIds(cut).ShouldNotContain(SirenPanels.SessionStats);
    }

    [Fact(DisplayName = "The Mock Server entry belongs to the mock server pages")]
    public void MockServerEntry_BelongsToTheMockServerPages()
    {
        var cut = RenderComponent<ContextPanel>();

        var mockServer = cut.FindComponents<RailItem>().Single(item => item.Instance.Id == SirenPanels.MockServer).Instance;
        mockServer.RoutePrefix.ShouldBe("/mock-server");
    }

    // The page that is open and the panel that is open are two states, and the rail must not draw them alike
    [Fact(DisplayName = "An entry lit for its page is not marked like an entry lit for its open panel")]
    public void PageEntry_IsNotMarkedLikeAnOpenPanel()
    {
        var cut = RenderComponent<ContextPanel>();

        var pageEntries = cut.FindComponents<RailItem>().Select(item => item.Instance)
            .Where(item => !string.IsNullOrEmpty(item.RoutePrefix)).ToList();

        pageEntries.ShouldNotBeEmpty();
        pageEntries.ShouldAllBe(item => item.ActiveRouteClass != item.ActivePanelClass);
    }

    // The layout owns the drawer the rail sits in, so a click that opens a panel has to reach it
    [Fact(DisplayName = "Clicking a panel's entry on the collapsed rail asks the layout to open the drawer")]
    public async Task ClickOnCollapsedRail_AsksTheLayoutToOpenTheDrawer()
    {
        var toggles = 0;
        var cut = RenderComponent<ContextPanel>(p => p.Add(x => x.DrawerToggled, () => toggles++));

        await cut.Find($"[data-panel-id={SirenPanels.History}] button").ClickAsync(new MouseEventArgs());

        toggles.ShouldBe(1);
    }
}
