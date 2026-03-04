using System.Threading.Channels;
using Hermes.Menu;
using Microsoft.Extensions.Logging;
using Siren.Components.NativeMenu;

namespace Siren.NativeMenu;

/// <summary>
/// Desktop implementation of native menu service using Hermes menu API.
/// Builds menu structure and writes click events to a Channel for async consumption.
/// </summary>
public class NativeMenuService : INativeMenuService
{
    private readonly ILogger<NativeMenuService> _logger;
    private readonly Channel<string> _clickChannel;

    private NativeMenuBar? _menuBar;
    private bool _isInitialized;

    // Keep reference to the environments submenu for dynamic updates
    private Hermes.Menu.NativeMenu? _environmentsSubmenu;

    public NativeMenuService(ILogger<NativeMenuService> logger)
    {
        _logger = logger;
        _clickChannel = Channel.CreateUnbounded<string>();
    }

    public bool IsActive => _isInitialized;

    public ChannelReader<string> MenuItemClicks => _clickChannel.Reader;

    public void Initialize(object menuBar)
    {
        if (_isInitialized)
            return;

        try
        {
            _menuBar = (NativeMenuBar)menuBar;
            BuildMenuStructure();
            _menuBar.ItemClicked += OnNativeMenuItemClicked;
            _isInitialized = true;
            _logger.LogInformation("Native menus initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize native menus");
        }
    }

    public void SetItemEnabled(string itemId, bool enabled)
    {
        if (_menuBar?.TryGetItem(itemId, out var item) == true && item is not null)
        {
            item.IsEnabled = enabled;
        }
    }

    public void SetItemLabel(string itemId, string label)
    {
        if (_menuBar?.TryGetItem(itemId, out var item) == true && item is not null)
        {
            item.Label = label;
        }
    }

    public void SetItemChecked(string itemId, bool isChecked)
    {
        if (_menuBar?.TryGetItem(itemId, out var item) == true && item is not null)
        {
            item.IsChecked = isChecked;
        }
    }

    public void AddEnvironmentMenuItem(string envName)
    {
        if (_environmentsSubmenu is null) return;

        var itemId = MenuItemIds.ToolsVariablesEnvPrefix + envName;
        _environmentsSubmenu.AddItem(envName, itemId);
    }

    public void RemoveEnvironmentMenuItem(string envName)
    {
        if (_environmentsSubmenu is null) return;

        var itemId = MenuItemIds.ToolsVariablesEnvPrefix + envName;
        _environmentsSubmenu.RemoveItem(itemId);
    }

    public void SetActiveEnvironment(string? envName)
    {
        if (_environmentsSubmenu is null) return;

        // Uncheck all environment items, then check the active one
        foreach (var item in _environmentsSubmenu.Items)
        {
            item.IsChecked = false;
        }

        if (envName is null)
        {
            // Check "None"
            if (_menuBar?.TryGetItem(MenuItemIds.ToolsVariablesEnvNone, out var noneItem) == true && noneItem is not null)
            {
                noneItem.IsChecked = true;
            }
        }
        else
        {
            var itemId = MenuItemIds.ToolsVariablesEnvPrefix + envName;
            if (_menuBar?.TryGetItem(itemId, out var envItem) == true && envItem is not null)
            {
                envItem.IsChecked = true;
            }
        }
    }

    public void RebuildEnvironmentMenu(IEnumerable<string> envNames, string? activeEnv)
    {
        if (_environmentsSubmenu is null) return;

        // Remove all existing items except "None"
        var existingIds = _environmentsSubmenu.Items
            .Where(i => i.Id != MenuItemIds.ToolsVariablesEnvNone)
            .Select(i => i.Id)
            .ToList();

        foreach (var id in existingIds)
        {
            _environmentsSubmenu.RemoveItem(id);
        }

        // Add all current environments
        foreach (var envName in envNames)
        {
            var itemId = MenuItemIds.ToolsVariablesEnvPrefix + envName;
            _environmentsSubmenu.AddItem(envName, itemId, item =>
                item.WithChecked(envName == activeEnv));
        }

        // Update "None" checked state
        if (_menuBar?.TryGetItem(MenuItemIds.ToolsVariablesEnvNone, out var noneItem) == true && noneItem is not null)
        {
            noneItem.IsChecked = activeEnv is null;
        }
    }

    public void RebuildPluginMenus(IReadOnlyList<NativePluginMenuData> plugins)
    {
        if (_menuBar is null) return;

        // Remove and recreate the entire Plugins menu
        try
        {
            _menuBar.RemoveMenu("Plugins");
        }
        catch
        {
            // Menu may not exist yet on first call
        }

        _menuBar.AddMenu("Plugins", menu =>
        {
            foreach (var plugin in plugins)
            {
                menu.AddSubmenu(plugin.PluginName, sub =>
                {
                    // Custom plugin menu items
                    foreach (var item in plugin.Items)
                    {
                        sub.AddItem(item.Text, item.ItemId, mi =>
                        {
                            if (item.Disabled) mi.WithEnabled(false);
                        });
                    }

                    if (plugin.Items.Count > 0)
                        sub.AddSeparator();

                    // Standard plugin actions
                    var aboutId = $"{MenuItemIds.ToolsPluginPrefix}{plugin.PluginId}.about";
                    var toggleId = $"{MenuItemIds.ToolsPluginPrefix}{plugin.PluginId}.toggle";
                    sub.AddItem("About...", aboutId);
                    sub.AddItem(plugin.IsEnabled ? "Disable" : "Enable", toggleId);
                });
            }

            if (plugins.Count > 0)
                menu.AddSeparator();

            menu.AddItem("View Plugins", MenuItemIds.ToolsPluginsView);
            menu.AddItem("Import Plugin...", MenuItemIds.ToolsPluginsImport);
        });
    }

    public void SetUpdateAvailable(string? version)
    {
        if (_menuBar?.TryGetItem(MenuItemIds.ToolsUpdates, out var item) == true && item is not null)
        {
            if (version is not null)
            {
                item.Label = $"Update Available: v{version}";
                item.IsEnabled = true;
            }
            else
            {
                item.Label = "Check for Updates";
                item.IsEnabled = true;
            }
        }
    }

    private void OnNativeMenuItemClicked(string itemId)
    {
        _logger.LogDebug("Menu item clicked: {ItemId}", itemId);
        _clickChannel.Writer.TryWrite(itemId);
    }

    private void BuildMenuStructure()
    {
        if (_menuBar is null)
            return;

        // App menu: On macOS, adds to system app menu. On Windows/Linux, creates "Siren" menu.
        _menuBar.AppMenu
            .AddItem("About Siren", MenuItemIds.SirenAbout, position: AppMenuPosition.Top)
            .AddSeparator(AppMenuPosition.AfterAbout)
            .AddItem("Settings...", MenuItemIds.SirenSettings, item =>
            {
                if (OperatingSystem.IsMacOS())
                    item.WithAccelerator("Cmd+,");
            })
            .AddSeparator()
            .AddItem("Report Issue", MenuItemIds.SirenReportIssue);

        // File menu
        _menuBar.AddMenu("File", menu =>
        {
            menu.AddItem("New Request", MenuItemIds.FileNew, item =>
                item.WithAccelerator("Ctrl+N"));
            menu.AddItem("Save Request", MenuItemIds.FileSave, item =>
                item.WithAccelerator("Ctrl+S"));
            menu.AddItem("Import Request", MenuItemIds.FileImport);
            menu.AddSeparator();
            menu.AddItem("Save Response", MenuItemIds.FileSaveResponse, item =>
                item.WithEnabled(false));
        });

        // Edit menu
        _menuBar.AddMenu("Edit", menu =>
        {
            menu.AddItem("Format Request", MenuItemIds.EditFormat);
        });

        // Tools menu
        _menuBar.AddMenu("Tools", menu =>
        {
            menu.AddSubmenu("cURL", sub =>
            {
                sub.AddItem("Import from cURL", MenuItemIds.ToolsCurlImport);
                sub.AddItem("Export as cURL", MenuItemIds.ToolsCurlExport, item =>
                    item.WithEnabled(false));
            });

            menu.AddSubmenu("History", sub =>
            {
                sub.AddItem("Export Last Request as HAR", MenuItemIds.ToolsHistoryExportLast, item =>
                    item.WithEnabled(false));
                sub.AddItem("Export All as HAR", MenuItemIds.ToolsHistoryExportAll);
                sub.AddSeparator();
                sub.AddItem("Clear History", MenuItemIds.ToolsHistoryClear);
            });

            menu.AddSubmenu("Collections", sub =>
            {
                sub.AddItem("Create New", MenuItemIds.ToolsCollectionsCreate);
                sub.AddSubmenu("Import From", importSub =>
                {
                    importSub.AddItem("OpenAPI", MenuItemIds.ToolsCollectionsImportOpenApi);
                    importSub.AddItem("Postman Collection", MenuItemIds.ToolsCollectionsImportPostman);
                    importSub.AddItem("Bruno Collection", MenuItemIds.ToolsCollectionsImportBruno);
                });
                sub.AddItem("Export to JSON", MenuItemIds.ToolsCollectionsExport);
            });

            menu.AddSubmenu("Variables", sub =>
            {
                sub.AddItem("Add Environment", MenuItemIds.ToolsVariablesAddEnv);
                sub.AddItem("Add Variable", MenuItemIds.ToolsVariablesAddVar);
                sub.AddSeparator();
                sub.AddSubmenu("Environments", envSub =>
                {
                    _environmentsSubmenu = envSub;
                    envSub.AddItem("None", MenuItemIds.ToolsVariablesEnvNone, item =>
                        item.WithChecked(true));
                });
            });

            menu.AddSeparator();
            menu.AddItem("Secret Manager", MenuItemIds.ToolsSecrets);
            menu.AddSeparator();
            menu.AddItem("Check for Updates", MenuItemIds.ToolsUpdates);
        });

        // Plugins menu (top-level)
        _menuBar.AddMenu("Plugins", menu =>
        {
            menu.AddItem("View Plugins", MenuItemIds.ToolsPluginsView);
            menu.AddItem("Import Plugin...", MenuItemIds.ToolsPluginsImport);
        });
    }
}
