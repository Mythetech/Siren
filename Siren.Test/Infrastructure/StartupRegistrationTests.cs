using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.Smoke;
using Siren.Smoke;

namespace Siren.Test.Infrastructure;

public class StartupRegistrationTests
{
    private static ServiceCollection AppServices()
    {
        var services = new ServiceCollection();
        Program.ConfigureServices(services, new ConfigurationBuilder().Build());
        return services;
    }

    [Fact]
    public void AppServices_Initialize_SettingsMigration_Then_Settings_Then_Plugins()
    {
        using var provider = AppServices().BuildServiceProvider();

        provider.GetRequiredService<IAsyncInitializationHost>().Should().NotBeNull();
        provider.GetServices<IAsyncInitializationHook>()
            .OrderBy(h => h.Order)
            .Select(h => h.Name)
            .Should().Equal("SettingsMigration", "Settings", "Plugins");
    }

    [Fact]
    public void AppServices_Register_Siren_SmokeChecks()
    {
        var checks = AppServices()
            .Where(d => d.ServiceType == typeof(ISmokeCheck))
            .Select(d => d.ImplementationType);

        checks.Should().BeEquivalentTo([typeof(DatabaseSmokeCheck), typeof(EditorScriptsSmokeCheck)]);
    }
}
