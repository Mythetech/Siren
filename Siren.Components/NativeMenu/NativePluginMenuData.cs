namespace Siren.Components.NativeMenu;

/// <summary>
/// Data transfer object for passing plugin menu information to the native menu service.
/// </summary>
public class NativePluginMenuData
{
    public required string PluginId { get; init; }
    public required string PluginName { get; init; }
    public bool IsEnabled { get; init; }
    public List<NativePluginMenuItemData> Items { get; init; } = [];
}

/// <summary>
/// Represents a single menu item from a plugin's PluginMenu component.
/// </summary>
public class NativePluginMenuItemData
{
    public required string Text { get; init; }
    public required string ItemId { get; init; }
    public bool Disabled { get; init; }
}
