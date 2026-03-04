using System.Threading.Channels;

namespace Siren.Components.NativeMenu;

/// <summary>
/// Service for managing native OS menus.
/// Interface defined in Components layer for platform-agnostic access.
/// Implementation lives in Siren project with access to Hermes.
/// </summary>
public interface INativeMenuService
{
    /// <summary>
    /// Whether native menus are active and should be used instead of Blazor menus.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Initialize and build the native menu structure.
    /// Should be called once during app startup after window is ready.
    /// </summary>
    /// <param name="menuBar">The native menu bar instance (platform-specific type).</param>
    void Initialize(object menuBar);

    /// <summary>
    /// Set whether a menu item is enabled.
    /// </summary>
    void SetItemEnabled(string itemId, bool enabled);

    /// <summary>
    /// Set the label text of a menu item.
    /// </summary>
    void SetItemLabel(string itemId, string label);

    /// <summary>
    /// Set whether a menu item is checked.
    /// </summary>
    void SetItemChecked(string itemId, bool isChecked);

    /// <summary>
    /// Channel reader for menu item clicks.
    /// Components should read from this channel to handle clicks asynchronously.
    /// </summary>
    ChannelReader<string> MenuItemClicks { get; }

    /// <summary>
    /// Add an environment menu item to the Variables > Environments submenu.
    /// </summary>
    void AddEnvironmentMenuItem(string envName);

    /// <summary>
    /// Remove an environment menu item from the Variables > Environments submenu.
    /// </summary>
    void RemoveEnvironmentMenuItem(string envName);

    /// <summary>
    /// Update the active environment checked state in the menu.
    /// </summary>
    void SetActiveEnvironment(string? envName);

    /// <summary>
    /// Rebuild the entire environments submenu from the provided list.
    /// </summary>
    void RebuildEnvironmentMenu(IEnumerable<string> envNames, string? activeEnv);

    /// <summary>
    /// Set the update available state. Pass null to hide, version string to show.
    /// </summary>
    void SetUpdateAvailable(string? version);

    /// <summary>
    /// Rebuild the Plugins menu with current plugin data.
    /// Each plugin gets a submenu with its custom items plus About/Enable/Disable.
    /// </summary>
    void RebuildPluginMenus(IReadOnlyList<NativePluginMenuData> plugins);
}
