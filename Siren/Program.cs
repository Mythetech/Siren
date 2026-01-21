using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Environment;
using Mythetech.Framework.Desktop.Photino;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Desktop.Updates;
using Photino.Blazor;
using Siren.Collections;
using Siren.Components;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.Settings;
using Siren.Components.Variables;
using Siren.History;
using Siren.Infrastructure;
using Siren.Mcp;
using Siren.MockServer;
using Siren.Variables;
using Velopack;

namespace Siren
{
    public class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            // Check for --mcp flag for stdio-based MCP server mode (used by Claude Desktop)
            if (args.Contains("--mcp"))
            {
                await RunMcpServerAsync(args);
                return;
            }

            VelopackApp.Build().Run();

            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            appBuilder.Services
                  .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddConsole();
                });

            appBuilder.Services.AddHttpClient();

            appBuilder.Services.AddDesktopServices();

            appBuilder.Services.AddUpdateService(options =>
            {
                var platform = OperatingSystem.IsWindows() ? "windows"
                    : OperatingSystem.IsMacOS() ? "macos"
                    : "linux";
                var channel = OperatingSystem.IsWindows() ? "win"
                    : OperatingSystem.IsMacOS() ? "osx"
                    : "linux";
                options.UpdateUrl = $"{Configuration.SirenDownloadConfiguration.UpdateBaseUrl}/{platform}";
                options.Channel = channel;
            });

            appBuilder.Services.AddDesktopSettingsStorage("Siren");
            appBuilder.Services.AddPluginStateProvider("Siren");

            appBuilder.Services.AddAllDesktopSecretManagers("siren");
            appBuilder.Services.AddSingleton<IVariableValueResolver, SecretReferenceResolver>();

            appBuilder.Services.AddPluginFramework();

            appBuilder.Services.AddRuntimeEnvironment(System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) ?? false ? DesktopRuntimeEnvironment.Production() : DesktopRuntimeEnvironment.Development());

            appBuilder.RootComponents.Add<Components.App>("app");

            appBuilder.Services.AddSirenComponents<HistoryService, CollectionsService, VariablesService, AppDataService, MockServerService>();

            // Settings storage for framework
            appBuilder.Services.AddSettingsStorage<LiteDbSettingsStorage>();

            appBuilder.Services.AddSirenMcp();

            appBuilder.Services.AddMessageBus(typeof(App).Assembly);

            var app = appBuilder.Build();

            app.Services.UseMessageBus(typeof(App).Assembly);

            app.Services
                .RegisterSettingsFromAssemblies(typeof(App).Assembly, typeof(DesktopHost).Assembly, typeof(SettingsBase).Assembly)
                .UseSettingsFramework();

            await SettingsMigration.MigrateIfNeededAsync(app.Services);
            await app.Services.LoadPersistedSettingsAsync();
            
            app.Services.UseSirenMcp();

            app.Services.UseSecretManager();

            app.Services.UsePlugins();

            app.MainWindow
                .SetSize(1920, 1080)
                .SetUseOsDefaultSize(false)
                .SetFullScreen(false)
                .SetLogVerbosity(0)
                .SetTitle("Siren");

            app.RegisterProvider(app.Services);

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
            };

            app.Run();
        }

        /// <summary>
        /// Runs Siren as a standalone MCP server using stdio transport.
        /// This is used by Claude Desktop and other MCP clients.
        /// </summary>
        private static async Task RunMcpServerAsync(string[] _)
        {
            var services = new ServiceCollection();

            services.AddLogging(builder => builder.AddConsole());
            services.AddHttpClient();
            services.AddMessageBus();

            services.AddSirenComponents<HistoryService, CollectionsService, VariablesService, AppDataService, MockServerService>();
            services.AddSettingsStorage<LiteDbSettingsStorage>();

            // Add MCP with stdio transport (default)
            services.AddMcp(options =>
            {
                options.ServerName = "Siren";
            });
            services.AddMcpTools(typeof(McpServiceExtensions).Assembly);

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.UseMessageBus();

            serviceProvider
                .RegisterSettings<HttpSettings>()
                .RegisterSettings<ResponseSettings>()
                .RegisterSettings<HistorySettings>()
                .RegisterSettings<EnvironmentSettings>()
                .RegisterSettings<PluginSettings>()
                .UseSettingsFramework();

            await SettingsMigration.MigrateIfNeededAsync(serviceProvider);
            await serviceProvider.LoadPersistedSettingsAsync();

            serviceProvider.UseMcp(typeof(McpServiceExtensions).Assembly);

            var server = serviceProvider.GetRequiredService<IMcpServer>();

            Console.Error.WriteLine("Siren MCP server starting...");
            await server.RunAsync();
            Console.Error.WriteLine("Siren MCP server stopped.");
        }
    }
}