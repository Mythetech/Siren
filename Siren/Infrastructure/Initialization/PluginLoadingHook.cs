using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;

namespace Siren.Infrastructure.Initialization;

/// <summary>
/// Loads plugins from the custom plugin directory when one is set, otherwise from the default one. It is a
/// hook rather than first-render work so plugins have finished loading before ApplicationReady, which is
/// what the framework/plugins smoke check waits on. Runs after settings load, because the directory is a
/// setting.
/// </summary>
public class PluginLoadingHook : IAsyncInitializationHook
{
    private readonly PluginState _pluginState;
    private readonly ISettingsProvider _settingsProvider;

    public PluginLoadingHook(PluginState pluginState, ISettingsProvider settingsProvider)
    {
        _pluginState = pluginState;
        _settingsProvider = settingsProvider;
    }

    public int Order => 200;

    public string Name => "Plugins";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_pluginState.PluginsLoaded)
            return;

        try
        {
            var pluginSettings = _settingsProvider.GetSettings<PluginSettings>();
            string? pluginDirectory = null;

            if (!string.IsNullOrWhiteSpace(pluginSettings?.CustomPluginDirectory)
                && Directory.Exists(pluginSettings.CustomPluginDirectory))
            {
                pluginDirectory = pluginSettings.CustomPluginDirectory;
            }

            await _pluginState.InitializePluginsAsync(pluginDirectory);
            await _pluginState.LoadStateAsync();
        }
        catch
        {
            // Plugins are optional, so a failed load must not stop startup. Individual plugin failures are
            // logged by the loader, and a load that throws never completes, which framework/plugins reports.
        }
    }
}
