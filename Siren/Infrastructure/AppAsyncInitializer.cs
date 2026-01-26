using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.Infrastructure;

namespace Siren.Infrastructure;

public class AppAsyncInitializer : IAppAsyncInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppAsyncInitializer> _logger;
    private int _initialized;

    public AppAsyncInitializer(
        IServiceProvider serviceProvider,
        ILogger<AppAsyncInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        _logger.LogDebug("Starting async initialization...");

        // Run settings migration first
        await StartServiceAsync("SettingsMigration", async () =>
        {
            await SettingsMigration.MigrateIfNeededAsync(_serviceProvider);
        });

        // Load persisted settings
        await StartServiceAsync("Settings", async () =>
        {
            await _serviceProvider.LoadPersistedSettingsAsync();
        });

        _logger.LogInformation("Async initialization complete");
    }

    private async Task StartServiceAsync(string serviceName, Func<Task> startFunc)
    {
        try
        {
            await startFunc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start {ServiceName}", serviceName);
        }
    }
}
