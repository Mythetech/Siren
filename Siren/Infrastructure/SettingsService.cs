using Siren.Components.Services;

namespace Siren.Infrastructure;

/// <summary>
/// Service for managing application data (purge, size queries).
/// Settings are now managed by Mythetech.Framework settings infrastructure.
/// </summary>
public class AppDataService : IAppDataService
{
    private const string DatabaseName = "siren.db";

    private static string GetDatabaseFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, DatabaseName);
    }

    public void PurgeSavedData()
    {
        var dbFilePath = GetDatabaseFilePath();

        if (File.Exists(dbFilePath))
        {
            File.Delete(dbFilePath);
        }
    }

    public long GetAppDataSize()
    {
        var dbFilePath = GetDatabaseFilePath();

        if (File.Exists(dbFilePath))
        {
            var fileInfo = new FileInfo(dbFilePath);
            return fileInfo.Length;
        }

        return 0;
    }
}

