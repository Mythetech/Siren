using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins.Components;
using Mythetech.Framework.Desktop.Updates.Components;
using Mythetech.Framework.Components.Secrets;
using Siren.Components.AppContextPanel;
using Siren.Components.Collections;
using Siren.Components.History;
using Siren.Components.Http.Commands;
using Siren.Components.NativeMenu;
using Siren.Components.NativeMenu.Commands;
using Siren.Components.Settings;
using Siren.Components.Shared.Dialogs.Commands;
using Siren.Components.Configuration;

namespace Siren.NativeMenu;

/// <summary>
/// Dispatches native menu item clicks by publishing commands to the MessageBus.
/// Keeps no references to UI services (ISnackbar, IDialogService, etc.) — those
/// are resolved by the consumer/provider components that handle each command.
/// </summary>
public class NativeMenuCommandDispatcher : INativeMenuCommandDispatcher
{
    private readonly IMessageBus _messageBus;
    private readonly ILinkOpenService _linkService;
    private readonly AppConfiguration _appConfig;
    private readonly ILogger<NativeMenuCommandDispatcher> _logger;

    private readonly Dictionary<string, Func<Task>> _handlers;

    public NativeMenuCommandDispatcher(
        IMessageBus messageBus,
        ILinkOpenService linkService,
        AppConfiguration appConfig,
        ILogger<NativeMenuCommandDispatcher> logger)
    {
        _messageBus = messageBus;
        _linkService = linkService;
        _appConfig = appConfig;
        _logger = logger;

        _handlers = new Dictionary<string, Func<Task>>
        {
            // App menu
            [MenuItemIds.SirenAbout] = () => ShowDialog<AboutSiren>("About", MaxWidth.Small),
            [MenuItemIds.SirenSettings] = () => ShowDialog<SettingsDialog>("Settings", MaxWidth.Large),
            [MenuItemIds.SirenReportIssue] = () => _linkService.OpenLinkAsync(_appConfig.ReportIssueUrl),

            // File menu
            [MenuItemIds.FileNew] = () => _messageBus.PublishAsync(new NewRequest()),
            [MenuItemIds.FileSave] = () => _messageBus.PublishAsync(new SaveActiveRequest()),
            [MenuItemIds.FileImport] = () => _messageBus.PublishAsync(new ImportRequest()),
            [MenuItemIds.FileSaveResponse] = () => _messageBus.PublishAsync(new SaveActiveResponse()),

            // Edit menu
            [MenuItemIds.EditFormat] = () => _messageBus.PublishAsync(new FormatActiveRequest()),

            // Tools > cURL
            [MenuItemIds.ToolsCurlImport] = () => ShowDialog<CurlImportDialog>("Import from cURL", MaxWidth.Medium),
            [MenuItemIds.ToolsCurlExport] = () => _messageBus.PublishAsync(new ExportActiveRequestAsCurl()),

            // Tools > History
            [MenuItemIds.ToolsHistoryExportLast] = () => _messageBus.PublishAsync(new ExportLastRequestAsHar()),
            [MenuItemIds.ToolsHistoryExportAll] = () => _messageBus.PublishAsync(new ExportAllAsHar()),
            [MenuItemIds.ToolsHistoryClear] = () => ShowDialog<DeleteHistoryDialog>("Delete History"),

            // Tools > Collections
            [MenuItemIds.ToolsCollectionsCreate] = () => _messageBus.PublishAsync(new CreateCollection()),
            [MenuItemIds.ToolsCollectionsImportOpenApi] = () => ShowDialog<ImportDialog>("Open API Import"),
            [MenuItemIds.ToolsCollectionsImportPostman] = () => _messageBus.PublishAsync(new Siren.Components.Collections.Commands.ImportPostmanCollection()),
            [MenuItemIds.ToolsCollectionsImportBruno] = () => _messageBus.PublishAsync(new Siren.Components.Collections.Commands.ImportBrunoCollection()),
            [MenuItemIds.ToolsCollectionsExport] = () => _messageBus.PublishAsync(new ExportCollectionsToJson()),

            // Tools > Variables
            [MenuItemIds.ToolsVariablesAddEnv] = () => _messageBus.PublishAsync(new AddEnvironment()),
            [MenuItemIds.ToolsVariablesAddVar] = () => _messageBus.PublishAsync(new AddVariable()),

            // Tools > Secrets
            [MenuItemIds.ToolsSecrets] = () => ShowDialog<SecretManagerDialog>("Secret Manager", MaxWidth.Large, fullWidth: true),

            // Tools > Plugins
            [MenuItemIds.ToolsPluginsView] = () => ShowDialog<PluginManagementDialog>("Plugin Management", MaxWidth.Large, fullWidth: true),
            [MenuItemIds.ToolsPluginsImport] = () => _messageBus.PublishAsync(new ImportPlugin()),

            // Tools > Updates
            [MenuItemIds.ToolsUpdates] = () => ShowDialog<UpdateProgressDialog>("Update Available", MaxWidth.Medium),
        };
    }

    public async Task HandleMenuItemClickAsync(string itemId)
    {
        try
        {
            if (_handlers.TryGetValue(itemId, out var handler))
            {
                await handler();
                return;
            }

            // Dynamic: environment selection
            if (itemId == MenuItemIds.ToolsVariablesEnvNone)
            {
                await _messageBus.PublishAsync(new SetActiveEnvironment(null));
                return;
            }

            if (itemId.StartsWith(MenuItemIds.ToolsVariablesEnvPrefix))
            {
                var envName = itemId[MenuItemIds.ToolsVariablesEnvPrefix.Length..];
                await _messageBus.PublishAsync(new SetActiveEnvironment(envName));
                return;
            }

            // Dynamic: plugin actions
            if (itemId.StartsWith(MenuItemIds.ToolsPluginPrefix))
            {
                var suffix = itemId[MenuItemIds.ToolsPluginPrefix.Length..];

                if (suffix.EndsWith(".about"))
                {
                    var pluginId = suffix[..^".about".Length];
                    await _messageBus.PublishAsync(new ShowAboutPlugin(pluginId));
                    return;
                }

                if (suffix.EndsWith(".toggle"))
                {
                    var pluginId = suffix[..^".toggle".Length];
                    await _messageBus.PublishAsync(new TogglePlugin(pluginId));
                    return;
                }

                // Custom plugin menu item — dispatch to callback registry
                await _messageBus.PublishAsync(new PluginMenuItemClicked(itemId));
                return;
            }

            _logger.LogWarning("Unhandled menu item click: {ItemId}", itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle menu item click: {ItemId}", itemId);
        }
    }

    private Task ShowDialog<TDialog>(string title, MaxWidth maxWidth = MaxWidth.Medium, bool fullWidth = false, DialogParameters? parameters = null)
        where TDialog : ComponentBase
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            BackgroundClass = "siren-dialog",
            MaxWidth = maxWidth,
            FullWidth = fullWidth
        };

        return _messageBus.PublishAsync(new ShowDialog(typeof(TDialog), title, options, parameters));
    }
}
