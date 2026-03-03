namespace Siren.Components.NativeMenu.Commands;

/// <summary>
/// Command published when a plugin's custom menu item is clicked.
/// The NativeMenuStateUpdater holds the callback mapping and invokes it.
/// </summary>
public record PluginMenuItemClicked(string ItemId);
