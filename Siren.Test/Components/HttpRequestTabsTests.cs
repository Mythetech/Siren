using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class HttpRequestTabsTests : TestContext
{
    private const string AddButton = "button[aria-label='Add tab']";
    private const string CloseButtons = "button[aria-label='Close tab']";
    private const string TabHeaders = ".mud-tab";

    private readonly RequestTabsState _tabs;

    public HttpRequestTabsTests()
    {
        _tabs = this.AddRequestTabs();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private SirenNetworkRequest Open(string path) => _tabs.Open(new SirenNetworkRequest
    {
        Request = new HttpRequest { RequestUri = $"https://api.example.com/{path}", Method = HttpMethod.Get }
    });

    private static string ActiveTabText(IRenderedComponent<HttpRequestTabs> cut)
        => cut.Find(".mud-tab-active").TextContent;

    [Fact(DisplayName = "The strip shows a tab for every open request")]
    public void Strip_ShowsEveryTab()
    {
        Open("alpha");
        Open("beta");

        var cut = RenderComponent<HttpRequestTabs>();

        cut.FindAll(TabHeaders).Count.ShouldBe(3);
    }

    [Fact(DisplayName = "The strip starts on the tab that is active in the state")]
    public void Strip_StartsOnActiveTab()
    {
        Open("alpha");
        Open("beta");

        var cut = RenderComponent<HttpRequestTabs>();

        cut.WaitForAssertion(() => ActiveTabText(cut).ShouldContain("/beta"));
    }

    [Fact(DisplayName = "A tab opened elsewhere becomes the active tab in the strip")]
    public void TabOpenedElsewhere_BecomesActive()
    {
        var cut = RenderComponent<HttpRequestTabs>();

        Open("opened");

        cut.WaitForAssertion(() => ActiveTabText(cut).ShouldContain("/opened"));
    }

    [Fact(DisplayName = "The add button opens a new tab and shows it as active")]
    public async Task AddButton_OpensActiveTab()
    {
        Open("alpha");
        var cut = RenderComponent<HttpRequestTabs>();

        await cut.Find(AddButton).ClickAsync(new MouseEventArgs());

        _tabs.Tabs.Count.ShouldBe(3);
        _tabs.Active.ShouldBeSameAs(_tabs.Tabs[^1]);
        cut.WaitForAssertion(() => cut.FindAll(TabHeaders)[^1].ClassList.ShouldContain("mud-tab-active"));
    }

    [Fact(DisplayName = "Clicking a tab makes it the active tab")]
    public async Task ClickingTab_ActivatesIt()
    {
        var first = _tabs.Active;
        Open("alpha");
        var cut = RenderComponent<HttpRequestTabs>();
        cut.WaitForAssertion(() => ActiveTabText(cut).ShouldContain("/alpha"));

        await cut.FindAll(TabHeaders)[0].ClickAsync(new MouseEventArgs());

        _tabs.Active.ShouldBeSameAs(first);
    }

    [Fact(DisplayName = "Closing the active tab shows the tab that takes its place")]
    public async Task ClosingActiveTab_ShowsTabInItsPlace()
    {
        var alpha = Open("alpha");
        var beta = Open("beta");
        _tabs.Activate(alpha.Id);
        var cut = RenderComponent<HttpRequestTabs>();
        cut.WaitForAssertion(() => ActiveTabText(cut).ShouldContain("/alpha"));

        await cut.FindAll(CloseButtons)[1].ClickAsync(new MouseEventArgs());

        _tabs.Tabs.ShouldNotContain(alpha);
        _tabs.Active.ShouldBeSameAs(beta);
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(TabHeaders).Count.ShouldBe(2);
            ActiveTabText(cut).ShouldContain("/beta");
        });
    }

    [Fact(DisplayName = "Closing the only tab shows a new blank tab")]
    public async Task ClosingOnlyTab_ShowsBlankTab()
    {
        var only = _tabs.Active;
        var cut = RenderComponent<HttpRequestTabs>();

        await cut.Find(CloseButtons).ClickAsync(new MouseEventArgs());

        _tabs.Tabs.ShouldHaveSingleItem().ShouldNotBeSameAs(only);
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(TabHeaders).Count.ShouldBe(1);
            cut.FindAll(".mud-tab-active").Count.ShouldBe(1);
        });
    }

    [Fact(DisplayName = "Duplicate works on a tab with no method")]
    public async Task Duplicate_WithoutMethod_OpensCopy()
    {
        var cut = RenderComponent<HttpRequestTabs>();
        var strip = cut.FindComponent<SirenDynamicTabs<SirenNetworkRequest>>();

        await cut.InvokeAsync(() => strip.Instance.TabDuplicated.InvokeAsync(_tabs.Active));

        _tabs.Tabs.Count.ShouldBe(2);
        cut.WaitForAssertion(() => cut.FindAll(TabHeaders).Count.ShouldBe(2));
    }

    [Fact(DisplayName = "A tab's label follows its request's URL")]
    public void TabLabel_FollowsUrl()
    {
        var cut = RenderComponent<HttpRequestTabs>();

        _tabs.SetUri(_tabs.Active.Id, "https://api.example.com/renamed");

        cut.WaitForAssertion(() => ActiveTabText(cut).ShouldContain("/renamed"));
    }
}
