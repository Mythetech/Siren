using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Guards;
using NSubstitute;
using Shouldly;
using Siren.Components;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class MonacoThemeSyncTests : TestContext
{
    private const string SetTheme = "blazorMonaco.editor.setTheme";

    private readonly SirenAppState _appState = TestState.AppState();
    private readonly IJsGuardService _guards = Substitute.For<IJsGuardService>();

    public MonacoThemeSyncTests()
    {
        Services.AddLogging();
        Services.AddSingleton(_appState);
        Services.AddSingleton(_guards);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "The Monaco theme is left alone until Monaco has loaded")]
    public void ColorModeChange_BeforeMonacoLoads_SetsNoTheme()
    {
        _guards.IsReady("monaco").Returns(false);
        var cut = RenderComponent<MonacoThemeSync>();

        _appState.IsDarkMode = true;

        cut.WaitForState(() => cut.RenderCount == 2);
        JSInterop.Invocations[SetTheme].ShouldBeEmpty();
    }

    [Theory(DisplayName = "A change of color mode sets the Monaco theme once Monaco has loaded")]
    [InlineData(true, "vs-dark")]
    [InlineData(false, "vs")]
    public void ColorModeChange_AfterMonacoLoads_SetsTheTheme(bool isDarkMode, string theme)
    {
        _guards.IsReady("monaco").Returns(true);
        var cut = RenderComponent<MonacoThemeSync>();

        _appState.IsDarkMode = isDarkMode;

        cut.WaitForAssertion(() => JSInterop.Invocations[SetTheme].ShouldHaveSingleItem().Arguments[0].ShouldBe(theme));
    }
}
