using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Components;

namespace Siren.Components.NativeMenu;

/// <summary>
/// Stores the mapping from native menu item IDs to plugin OnClick callbacks.
/// Populated by NativeMenuStateUpdater when plugin menus are rebuilt.
/// Consumed by MenuCommandProvider when a plugin menu item is clicked.
/// </summary>
public class PluginMenuCallbackRegistry
{
    private readonly Dictionary<string, Func<PluginContext, Task>> _callbacks = new();

    public void Clear() => _callbacks.Clear();

    public void Register(string itemId, Func<PluginContext, Task> callback)
    {
        _callbacks[itemId] = callback;
    }

    public async Task<bool> TryInvoke(string itemId, PluginContext context)
    {
        if (_callbacks.TryGetValue(itemId, out var callback))
        {
            await callback(context);
            return true;
        }
        return false;
    }
}
