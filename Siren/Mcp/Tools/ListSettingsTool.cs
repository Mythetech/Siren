using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.Settings;

namespace Siren.Mcp.Tools;

[McpTool(Name = "list_settings", Description = "List the current Siren application settings including request timeout, display preferences, and default values")]
public class ListSettingsTool : IMcpTool
{
    private readonly ISettingsProvider _settingsProvider;

    public ListSettingsTool(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpSettings = _settingsProvider.GetSettings<HttpSettings>();
            var responseSettings = _settingsProvider.GetSettings<ResponseSettings>();
            var historySettings = _settingsProvider.GetSettings<HistorySettings>();
            var environmentSettings = _settingsProvider.GetSettings<EnvironmentSettings>();
            var pluginSettings = _settingsProvider.GetSettings<PluginSettings>();

            var result = new
            {
                requestTimeout = httpSettings?.RequestTimeout ?? 10,
                sendRequestsWithSystemToken = httpSettings?.SendRequestsWithSystemToken ?? true,
                saveHttpContent = historySettings?.SaveHttpContent ?? false,
                timeDisplay = responseSettings?.TimeDisplay.ToString() ?? "Milliseconds",
                sizeDisplay = responseSettings?.SizeDisplay.ToString() ?? "Bytes",
                defaultUserAgent = httpSettings?.DefaultUserAgent ?? "siren/0.1",
                defaultHttpMethod = httpSettings?.DefaultHttpMethod,
                defaultEnvironment = environmentSettings?.DefaultEnvironment,
                pluginsActive = pluginSettings?.PluginsActive ?? false
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to load settings: {ex.Message}"));
        }
    }
}
