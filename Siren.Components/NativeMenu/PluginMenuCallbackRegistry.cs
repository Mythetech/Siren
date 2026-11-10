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
    private readonly record struct Entry(Func<PluginContext, Task> Callback, string? PluginName);

    private readonly PluginMenuItemInvoker _invoker;
    private readonly Dictionary<string, Entry> _callbacks = new();

    public PluginMenuCallbackRegistry(PluginMenuItemInvoker invoker)
    {
        _invoker = invoker;
    }

    public void Clear() => _callbacks.Clear();

    public void Register(string itemId, Func<PluginContext, Task> callback, string? pluginName = null)
    {
        _callbacks[itemId] = new Entry(callback, pluginName);
    }

    public async Task<bool> TryInvoke(string itemId, PluginContext context)
    {
        if (!_callbacks.TryGetValue(itemId, out var entry))
            return false;

        return await _invoker.InvokeAsync(entry.Callback, context, entry.PluginName);
    }
}
