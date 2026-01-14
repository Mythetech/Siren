using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Settings;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Siren.Infrastructure;

/// <summary>
/// Handles one-time migration of legacy PersistentSettings to framework settings format.
/// </summary>
public static class SettingsMigration
{
    private const string DatabaseName = "siren.db";
    private const string LegacyCollectionName = "PersistentSettings";
    private const string MigrationMarkerKey = "_settings_migrated_v1";

    public static async Task MigrateIfNeededAsync(IServiceProvider services)
    {
        var storage = services.GetService<ISettingsStorage>();
        if (storage == null)
        {
            System.Diagnostics.Debug.WriteLine("Settings migration: No storage available");
            return;
        }

        // Check if already migrated
        var marker = await storage.LoadSettingsAsync(MigrationMarkerKey);
        if (marker != null)
        {
            System.Diagnostics.Debug.WriteLine("Settings migration: Already migrated");
            return;
        }

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DatabaseName);

        if (!File.Exists(dbPath))
        {
            // No existing database, mark as migrated
            await storage.SaveSettingsAsync(MigrationMarkerKey, "true");
            System.Diagnostics.Debug.WriteLine("Settings migration: No existing database");
            return;
        }

        try
        {
            using var db = new LiteDatabase(dbPath);
            var collection = db.GetCollection<BsonDocument>(LegacyCollectionName);
            var legacy = collection.FindById(1);

            if (legacy != null)
            {
                System.Diagnostics.Debug.WriteLine("Settings migration: Found legacy settings, migrating...");

                // Migrate to HttpSettings
                var httpSettings = new Dictionary<string, object?>
                {
                    ["SendRequestsWithSystemToken"] = GetBool(legacy, "SendRequestsWithSystemToken", true),
                    ["DefaultUserAgent"] = GetString(legacy, "DefaultUserAgent", "siren/0.1"),
                    ["DefaultHttpMethod"] = GetNullableString(legacy, "DefaultHttpMethod"),
                    ["RequestTimeout"] = GetInt(legacy, "RequestTimeout", 10),
                    ["RetryAttempts"] = GetInt(legacy, "RetryAttempts", 0),
                    ["RetryDelayMs"] = GetInt(legacy, "RetryDelayMs", 1000)
                };
                await storage.SaveSettingsAsync("Http", JsonSerializer.Serialize(httpSettings));

                // Migrate to ResponseSettings
                var responseSettings = new Dictionary<string, object?>
                {
                    ["TimeDisplay"] = GetInt(legacy, "TimeDisplay", 0),
                    ["SizeDisplay"] = GetInt(legacy, "SizeDisplay", 0)
                };
                await storage.SaveSettingsAsync("Response", JsonSerializer.Serialize(responseSettings));

                // Migrate to HistorySettings
                var historySettings = new Dictionary<string, object?>
                {
                    ["SaveHttpContent"] = GetBool(legacy, "SaveHttpContent", false)
                };
                await storage.SaveSettingsAsync("History", JsonSerializer.Serialize(historySettings));

                // Migrate to EnvironmentSettings
                var environmentSettings = new Dictionary<string, object?>
                {
                    ["DefaultEnvironment"] = GetNullableString(legacy, "LastActiveEnvironment")
                };
                await storage.SaveSettingsAsync("Environment", JsonSerializer.Serialize(environmentSettings));

                // Migrate PluginState to framework's PluginSettings
                var pluginSettings = new Dictionary<string, object?>
                {
                    ["PluginsActive"] = GetBool(legacy, "PluginState", false)
                };
                await storage.SaveSettingsAsync("Plugins", JsonSerializer.Serialize(pluginSettings));

                System.Diagnostics.Debug.WriteLine("Settings migration: Complete");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Settings migration: No legacy settings found");
            }

            // Mark migration complete
            await storage.SaveSettingsAsync(MigrationMarkerKey, "true");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings migration failed: {ex}");
            // Continue without migration - users will get defaults
        }
    }

    private static bool GetBool(BsonDocument doc, string key, bool defaultValue)
    {
        return doc.TryGetValue(key, out var value) && !value.IsNull ? value.AsBoolean : defaultValue;
    }

    private static int GetInt(BsonDocument doc, string key, int defaultValue)
    {
        return doc.TryGetValue(key, out var value) && !value.IsNull ? value.AsInt32 : defaultValue;
    }

    private static string GetString(BsonDocument doc, string key, string defaultValue)
    {
        return doc.TryGetValue(key, out var value) && !value.IsNull ? value.AsString : defaultValue;
    }

    private static string? GetNullableString(BsonDocument doc, string key)
    {
        return doc.TryGetValue(key, out var value) && !value.IsNull ? value.AsString : null;
    }
}
