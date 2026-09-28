using Mythetech.Framework.Infrastructure.Initialization;

namespace Siren.Infrastructure.Initialization;

/// <summary>
/// Moves legacy settings into the Framework settings store. Runs before the Framework's
/// <see cref="SettingsInitializationHook"/> (order 100) so the migrated values are what gets loaded.
/// </summary>
public class SettingsMigrationHook : IAsyncInitializationHook
{
    private readonly IServiceProvider _serviceProvider;

    public SettingsMigrationHook(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public int Order => 50;

    public string Name => "SettingsMigration";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SettingsMigration.MigrateIfNeededAsync(_serviceProvider);
    }
}
