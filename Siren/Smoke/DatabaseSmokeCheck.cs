using LiteDB;
using Mythetech.Framework.Infrastructure.Smoke;

namespace Siren.Smoke;

/// <summary>
/// Passes when the Siren database, which holds history, collections, variables and mock routes, exists after
/// startup, opens, and lists its collections. Startup only reads it for the environment list, so this covers
/// the store the history, collections and mock server panels depend on even when they have not rendered.
/// Opens it read-only, so a local smoke run leaves the developer's data untouched.
/// </summary>
public sealed class DatabaseSmokeCheck : ISmokeCheck
{
    private const string DatabaseName = "siren.db";

    public string Name => "siren/database";

    public Task RunAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DatabaseName);

        if (!File.Exists(path))
            throw new FileNotFoundException("The Siren database was not created at startup", path);

        // Not moved to Task.Run: Hermes runs checks on the renderer thread, where most of the app's own reads of
        // this file happen, and the repositories open it per call with no shared lock to serialise against.
        using var db = new LiteDatabase(new ConnectionString { Filename = path, ReadOnly = true });
        _ = db.GetCollectionNames().ToList();

        return Task.CompletedTask;
    }
}
