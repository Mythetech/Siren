using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Settings;

namespace Siren.Test.Infrastructure;

public class SettingsStorageRegistrationTests
{
    [Fact]
    public void AppServices_Resolve_SettingsStorage()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        Program.ConfigureServices(services, configuration);
        using var provider = services.BuildServiceProvider();

        var storage = provider.GetService<ISettingsStorage>();

        storage.Should().NotBeNull();
    }

    [Fact]
    public void McpServices_Resolve_SettingsStorage()
    {
        var services = new ServiceCollection();
        Program.ConfigureMcpServices(services);
        using var provider = services.BuildServiceProvider();

        var storage = provider.GetService<ISettingsStorage>();

        storage.Should().NotBeNull();
    }
}
