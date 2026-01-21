using LiteDB;
using Siren.Components.MockServer.Models;

namespace Siren.MockServer;

internal static class MockServerRepository
{
    private static readonly string DatabaseName = "siren.db";

    private static string GetPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static LiteDatabase GetDatabase()
    {
        return new LiteDatabase(Path.Combine(GetPath(), DatabaseName));
    }

    public static List<MockServerConfiguration> GetConfigurations()
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<PersistentMockServerConfiguration>();

        return collection.FindAll()
            .Select(x => x.ToConfiguration())
            .ToList();
    }

    public static MockServerConfiguration? GetConfiguration(Guid id)
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<PersistentMockServerConfiguration>();

        var config = collection.FindOne(x => x.Id == id);
        return config?.ToConfiguration();
    }

    public static void UpsertConfiguration(MockServerConfiguration configuration)
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<PersistentMockServerConfiguration>();

        collection.Upsert(PersistentMockServerConfiguration.Create(configuration));
    }

    public static void DeleteConfiguration(Guid id)
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<PersistentMockServerConfiguration>();

        collection.Delete(id);
    }

    public static void DeleteAllConfigurations()
    {
        using var db = GetDatabase();
        var collection = db.GetCollection<PersistentMockServerConfiguration>();

        collection.DeleteAll();
    }
}
