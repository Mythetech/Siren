using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Siren.Components.Http.Models;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class SirenDynamicTabsTests : TestContext
{
    private readonly List<SirenNetworkRequest> _items = [Tab("alpha"), Tab("beta"), Tab("gamma")];
    private readonly List<SirenNetworkRequest> _activated = [];

    public SirenDynamicTabsTests()
    {
        // Each tab header shows its method in a chip, which reads the app state
        Services.AddSingleton(TestState.AppState());
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private static SirenNetworkRequest Tab(string path) => new()
    {
        Request = new HttpRequest { RequestUri = $"https://api.example.com/{path}", Method = HttpMethod.Get }
    };

    private IRenderedComponent<SirenDynamicTabs<SirenNetworkRequest>> RenderStrip()
        => RenderComponent<SirenDynamicTabs<SirenNetworkRequest>>(p => p
            .Add(x => x.Items, _items)
            .Add(x => x.TabChanged, (SirenNetworkRequest item) => _activated.Add(item)));

    private static Task ReportIndexAsync(IRenderedComponent<SirenDynamicTabs<SirenNetworkRequest>> cut, int index)
    {
        var bar = cut.FindComponent<SirenDynamicTabBar>();
        return cut.InvokeAsync(() => bar.Instance.ActivePanelIndexChanged.InvokeAsync(index));
    }

    [Fact(DisplayName = "An index means the tab whose panel is at that place in the strip")]
    public async Task Index_MeansPanelsTab()
    {
        var cut = RenderStrip();
        var gamma = _items[2];
        _items.RemoveAt(1);

        await ReportIndexAsync(cut, 2);

        _activated.ShouldBe([gamma]);
    }

    [Fact(DisplayName = "An index whose panel belongs to a tab that is gone activates nothing")]
    public async Task IndexOfGoneTab_ActivatesNothing()
    {
        var cut = RenderStrip();
        _items.RemoveAt(1);

        await ReportIndexAsync(cut, 1);

        _activated.ShouldBeEmpty();
    }
}
