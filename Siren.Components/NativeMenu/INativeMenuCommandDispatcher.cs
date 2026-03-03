namespace Siren.Components.NativeMenu;

/// <summary>
/// Dispatches native menu item clicks to the appropriate async commands.
/// </summary>
public interface INativeMenuCommandDispatcher
{
    /// <summary>
    /// Handle a menu item click by dispatching the appropriate command.
    /// </summary>
    /// <param name="itemId">The ID of the clicked menu item.</param>
    Task HandleMenuItemClickAsync(string itemId);
}
