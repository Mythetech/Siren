using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Siren.Components;
using Siren.Components.MockServer;
using Siren.Components.MockServer.Models;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class MockServerEditTests : TestContext
{
    private const string Create = "blazorMonaco.editor.create";

    private readonly SirenAppState _appState = TestState.AppState();

    private readonly MockServerConfiguration _configuration = new()
    {
        Endpoints = [new MockEndpoint { Name = "Users", RoutePattern = "/users" }],
    };

    public MockServerEditTests()
    {
        var service = Substitute.For<IMockServerService>();
        service.GetConfiguration(_configuration.Id).Returns(_configuration);
        service.GetConfigurations().Returns([_configuration]);

        Services.AddLogging();
        Services.AddSingleton(_appState);
        Services.AddSingleton(new MockServerState(service));
        this.AddReadyJsGuards();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    [Theory(DisplayName = "The mock response editor takes the theme of the app's color mode")]
    [InlineData(true, "vs-dark")]
    [InlineData(false, "vs")]
    public void ResponseEditor_TakesTheAppsColorMode(bool isDarkMode, string theme)
    {
        _appState.IsDarkMode = isDarkMode;

        RenderComponent<MockServerEdit>(p => p.Add(x => x.Id, _configuration.Id));

        var options = (JsonElement)JSInterop.Invocations[Create].ShouldHaveSingleItem().Arguments[1]!;
        options.GetProperty("theme").GetString().ShouldBe(theme);
    }
}
