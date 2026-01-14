namespace Siren.Components.Services;

/// <summary>
/// Service for managing application data (purge, size queries).
/// Settings are now managed by Mythetech.Framework.
/// </summary>
public interface IAppDataService
{
    void PurgeSavedData();
    long GetAppDataSize();
}

