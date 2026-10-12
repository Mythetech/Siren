namespace Siren.Components.Shared;

/// <summary>
/// Names the Monaco theme that goes with the app's color mode.
/// </summary>
public static class MonacoTheme
{
    public static string For(bool isDarkMode) => isDarkMode ? "vs-dark" : "vs";
}
