using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.Services;

namespace Siren.Mcp.Tools;

[McpTool(Name = "list_settings", Description = "List the current Siren application settings including request timeout, display preferences, and default values")]
public class ListSettingsTool : IMcpTool
{
    private readonly ISettingsService _settingsService;

    public ListSettingsTool(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _settingsService.LoadSettings();

            if (settings == null)
            {
                return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(new
                {
                    message = "No settings found, using defaults",
                    requestTimeout = 10,
                    sendRequestsWithSystemToken = true,
                    saveHttpContent = false,
                    timeDisplay = "Milliseconds",
                    sizeDisplay = "Bytes",
                    defaultUserAgent = "siren/0.1",
                    defaultHttpMethod = (string?)null,
                    lastActiveEnvironment = (string?)null,
                    pluginState = false
                }, new JsonSerializerOptions { WriteIndented = true })));
            }

            var result = new
            {
                requestTimeout = settings.RequestTimeout,
                sendRequestsWithSystemToken = settings.SendRequestsWithSystemToken,
                saveHttpContent = settings.SaveHttpContent,
                timeDisplay = settings.TimeDisplay.ToString(),
                sizeDisplay = settings.SizeDisplay.ToString(),
                defaultUserAgent = settings.DefaultUserAgent,
                defaultHttpMethod = settings.DefaultHttpMethod,
                lastActiveEnvironment = settings.LastActiveEnvironment,
                pluginState = settings.PluginState
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to load settings: {ex.Message}"));
        }
    }
}
