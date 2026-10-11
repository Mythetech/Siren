using Shouldly;
using Siren.Components;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Test.Infrastructure;

namespace Siren.Test.Services;

/// <summary>
/// While components move to RequestTabsState one group at a time, the old members on SirenAppState have
/// to read and write the same tabs. The task that deletes those members deletes this file with them.
/// </summary>
public class SirenAppStateTabsTests
{
    private readonly RequestTabsState _tabs = TestState.Tabs();
    private readonly SirenAppState _appState;

    public SirenAppStateTabsTests()
    {
        _appState = TestState.AppState(_tabs);
    }

    private static SirenNetworkRequest Request() => new()
    {
        Request = new HttpRequest { RequestUri = "https://api.example.com/users" }
    };

    [Fact(DisplayName = "A request added through the app state is opened in the tabs state")]
    public void AddNetworkRequest_OpensTab()
    {
        var request = Request();

        _appState.AddNetworkRequest(request);

        _tabs.Tabs.ShouldContain(request);
        _tabs.Active.ShouldBeSameAs(request);
        _appState.Active.ShouldBeSameAs(request);
        _appState.NetworkRequests.ShouldBe(_tabs.Tabs);
    }

    [Fact(DisplayName = "A request removed through the app state is closed in the tabs state")]
    public void RemoveNetworkRequest_ClosesTab()
    {
        var request = Request();
        _appState.AddNetworkRequest(request);

        _appState.RemoveNetworkRequest(request.Id);

        _tabs.Tabs.ShouldNotContain(request);
    }

    [Fact(DisplayName = "Activating through the app state activates the tab in the tabs state")]
    public void SetActive_ActivatesTab()
    {
        var first = _tabs.Active;
        _appState.AddNetworkRequest(Request());

        _appState.SetActive(first.Id);

        _tabs.Active.ShouldBeSameAs(first);
    }

    [Fact(DisplayName = "A change made through the tabs state raises the app state's change event")]
    public void TabsStateChange_RaisesOnAppStateChanged()
    {
        var raised = 0;
        _appState.OnAppStateChanged += _ => raised++;

        _tabs.SetUri(_tabs.Active.Id, "https://api.example.com/users");

        raised.ShouldBe(1);
    }

    [Fact(DisplayName = "A change made through the tabs state does not look like a tab switch")]
    public void TabsStateChange_DoesNotRaiseActiveRequestChanged()
    {
        var raised = 0;
        _appState.OnActiveNetworkRequestChanged += _ => raised++;

        _tabs.SetUri(_tabs.Active.Id, "https://api.example.com/users");

        raised.ShouldBe(0);
    }

    [Fact(DisplayName = "A response stored through the tabs state raises the app state's active request event")]
    public void TabsStateResponse_RaisesOnActiveNetworkRequestChanged()
    {
        var raised = 0;
        _appState.OnActiveNetworkRequestChanged += _ => raised++;

        _tabs.SetResponse(_tabs.Active.Id, new RequestResult(), "{}");

        raised.ShouldBe(1);
    }
}
