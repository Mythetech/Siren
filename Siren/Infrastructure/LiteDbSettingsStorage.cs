using LiteDB;
using Mythetech.Framework.Infrastructure.Settings;

namespace Siren.Infrastructure;

/// <summary>
/// LiteDB implementation of ISettingsStorage for persisting framework settings.
/// Uses a separate collection from other app data for clean separation.
/// </summary>
public class LiteDbSettingsStorage : ISettingsStorage
{
    private const string DatabaseName = "siren.db";
    private const string CollectionName = "framework_settings";

    private static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, DatabaseName);
    }

    private LiteDatabase GetDatabase() => new(GetDatabasePath());

    public Task SaveSettingsAsync(string settingsId, string jsonData)
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<SettingsDocument>(CollectionName);

        var doc = new SettingsDocument
        {
            SettingsId = settingsId,
            JsonData = jsonData,
            UpdatedAt = DateTime.UtcNow
        };

        collection.Upsert(doc);
        System.Diagnostics.Debug.WriteLine($"Settings saved: {settingsId}");
        return Task.CompletedTask;
    }

    public Task<string?> LoadSettingsAsync(string settingsId)
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<SettingsDocument>(CollectionName);

        var doc = collection.FindById(settingsId);
        return Task.FromResult(doc?.JsonData);
    }

    public Task<Dictionary<string, string>> LoadAllSettingsAsync()
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<SettingsDocument>(CollectionName);

        var result = collection.FindAll()
            .ToDictionary(d => d.SettingsId, d => d.JsonData);

        System.Diagnostics.Debug.WriteLine($"Loaded {result.Count} settings domains");
        return Task.FromResult(result);
    }

    private class SettingsDocument
    {
        [BsonId]
        public string SettingsId { get; set; } = "";
        public string JsonData { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }
}
