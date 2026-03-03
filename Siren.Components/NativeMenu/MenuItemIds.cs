namespace Siren.Components.NativeMenu;

/// <summary>
/// Centralized menu item ID constants for native menus.
/// Uses hierarchical dot-notation for organization.
/// </summary>
public static class MenuItemIds
{
    // App menu (macOS system menu / "Siren" menu on Windows)
    public const string SirenAbout = "siren.about";
    public const string SirenSettings = "siren.settings";
    public const string SirenReportIssue = "siren.reportissue";

    // File menu
    public const string FileNew = "file.new";
    public const string FileSave = "file.save";
    public const string FileImport = "file.import";
    public const string FileSaveResponse = "file.saveresponse";

    // Edit menu
    public const string EditFormat = "edit.format";

    // Tools > cURL
    public const string ToolsCurlImport = "tools.curl.import";
    public const string ToolsCurlExport = "tools.curl.export";

    // Tools > History
    public const string ToolsHistoryExportLast = "tools.history.exportlast";
    public const string ToolsHistoryExportAll = "tools.history.exportall";
    public const string ToolsHistoryClear = "tools.history.clear";

    // Tools > Collections
    public const string ToolsCollectionsCreate = "tools.collections.create";
    public const string ToolsCollectionsImportOpenApi = "tools.collections.import.openapi";
    public const string ToolsCollectionsImportPostman = "tools.collections.import.postman";
    public const string ToolsCollectionsImportBruno = "tools.collections.import.bruno";
    public const string ToolsCollectionsExport = "tools.collections.export";

    // Tools > Variables
    public const string ToolsVariablesAddEnv = "tools.variables.addenv";
    public const string ToolsVariablesAddVar = "tools.variables.addvar";
    public const string ToolsVariablesEnvNone = "tools.variables.env.none";

    // Tools > Secrets
    public const string ToolsSecrets = "tools.secrets";

    // Tools > Plugins
    public const string ToolsPluginsView = "tools.plugins.view";
    public const string ToolsPluginsImport = "tools.plugins.import";

    // Tools > Updates
    public const string ToolsUpdates = "tools.updates";

    // Dynamic ID prefixes
    public const string ToolsVariablesEnvPrefix = "tools.variables.env.";
    public const string ToolsPluginPrefix = "tools.plugins.";
    public const string ToolsMcpPrefix = "tools.mcp.";
}
