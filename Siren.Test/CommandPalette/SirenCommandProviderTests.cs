using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NSubstitute;
using Siren.Components;
using Siren.Components.CommandPalette;
using Siren.Components.Theme;

namespace Siren.Test.CommandPalette;

public class SirenCommandProviderTests
{
    private readonly SirenAppState _appState = new();
    private readonly NavigationManager _nav = Substitute.For<NavigationManager>();
    private readonly SirenCommandProvider _provider;

    public SirenCommandProviderTests()
    {
        _provider = new SirenCommandProvider(_appState, _nav);
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
}
