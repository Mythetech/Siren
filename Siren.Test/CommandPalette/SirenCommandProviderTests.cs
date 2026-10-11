using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using Siren.Components;
using Siren.Components.CommandPalette;
using Siren.Components.Http;
using Siren.Components.Theme;
using Siren.Test.Infrastructure;

namespace Siren.Test.CommandPalette;

public class SirenCommandProviderTests
{
    private readonly RequestTabsState _tabs = TestState.Tabs();
    private readonly SirenAppState _appState;
    private readonly TestNavigationManager _nav = new();
    private readonly IRequestSender _sender = Substitute.For<IRequestSender>();
    private readonly SirenCommandProvider _provider;

    public SirenCommandProviderTests()
    {
        _appState = TestState.AppState();
        _provider = new SirenCommandProvider(_appState, _nav, _tabs, _sender);
    }

    // The real NavigationManager is abstract and refuses to work until it is given a base address
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }

    private async Task InvokeAsync(string commandId)
    {
        var commands = await _provider.GetCommandsAsync("", CancellationToken.None);
        await commands.Single(c => c.Id == commandId).InvokeAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetCommandsAsync_ReturnsAllExpectedCommands()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Should().HaveCount(12);
    }

    [Fact]
    public async Task GetCommandsAsync_ContainsNavigationCommands()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Should().Contain(c => c.Id == "nav.home");
        result.Should().Contain(c => c.Id == "nav.mock-server");
    }

    [Fact]
    public async Task GetCommandsAsync_ContainsPanelCommands()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Should().Contain(c => c.Id == "panel.history");
        result.Should().Contain(c => c.Id == "panel.collections");
        result.Should().Contain(c => c.Id == "panel.variables");
        result.Should().Contain(c => c.Id == "panel.mock-server");
    }

    [Fact]
    public async Task GetCommandsAsync_ContainsRequestContextCommands()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Should().Contain(c => c.Id == "context.auth");
        result.Should().Contain(c => c.Id == "context.headers");
        result.Should().Contain(c => c.Id == "context.cookies");
    }

    [Fact]
    public async Task GetCommandsAsync_ContainsActionCommands()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Should().Contain(c => c.Id == "action.send-request");
        result.Should().Contain(c => c.Id == "action.new-tab");
    }

    [Fact]
    public async Task GetCommandsAsync_ContainsSettingsCommand()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Should().Contain(c => c.Id == "app.settings");
    }

    [Fact]
    public async Task GetCommandsAsync_CommandsHaveCorrectGroups()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Where(c => c.Id.StartsWith("nav.")).Should().OnlyContain(c => c.Group == "Navigation");
        result.Where(c => c.Id.StartsWith("panel.")).Should().OnlyContain(c => c.Group == "Panels");
        result.Where(c => c.Id.StartsWith("context.")).Should().OnlyContain(c => c.Group == "Request Context");
        result.Where(c => c.Id.StartsWith("action.")).Should().OnlyContain(c => c.Group == "Actions");
        result.Where(c => c.Id.StartsWith("app.")).Should().OnlyContain(c => c.Group == "Settings");
    }

    [Fact]
    public async Task GetCommandsAsync_AllCommandsHaveIcons()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Should().OnlyContain(c => !string.IsNullOrEmpty(c.Icon));
    }

    [Fact]
    public async Task GetCommandsAsync_AllCommandsHaveUniqueIds()
    {
        var result = await _provider.GetCommandsAsync("", CancellationToken.None);

        result.Select(c => c.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task SendRequest_SendsTheActiveTab()
    {
        await InvokeAsync("action.send-request");

        await _sender.Received(1).SendAsync(_tabs.Active.Id);
    }

    [Fact]
    public async Task SendRequest_GoesToTheRequestsPage()
    {
        _nav.NavigateTo("/mock-server");

        await InvokeAsync("action.send-request");

        _nav.ToBaseRelativePath(_nav.Uri).Should().BeEmpty();
    }

    [Fact]
    public async Task NewTab_OpensAndActivatesATab()
    {
        var before = _tabs.Active;
        _nav.NavigateTo("/mock-server");

        await InvokeAsync("action.new-tab");

        _tabs.Tabs.Should().HaveCount(2);
        _tabs.Active.Should().NotBeSameAs(before);
        _nav.ToBaseRelativePath(_nav.Uri).Should().BeEmpty();
    }
}
