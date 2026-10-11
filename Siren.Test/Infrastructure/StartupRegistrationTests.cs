using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.Smoke;
using Siren.Components.Http;
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

    [Fact]
    public void AppServices_Register_One_RequestTabsState_For_The_Whole_App()
    {
        AppServices().Should().ContainSingle(d => d.ServiceType == typeof(RequestTabsState))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AppServices_Register_The_RequestComposer()
    {
        AppServices().Should().ContainSingle(d => d.ServiceType == typeof(IRequestComposer))
            .Which.ImplementationType.Should().Be(typeof(RequestComposer));
    }

    [Fact]
    public void McpServices_Log_To_StandardError_Because_Stdout_Carries_The_Protocol()
    {
        var services = new ServiceCollection();
        Program.ConfigureMcpServices(services);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value
            .LogToStandardErrorThreshold.Should().Be(LogLevel.Trace);
    }
}
