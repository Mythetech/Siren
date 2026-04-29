using Hermes;
using Hermes.Blazor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Environment;
using Mythetech.Framework.Desktop.Hermes;
using Mythetech.Framework.Infrastructure.Guards;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Desktop.Updates;
using Siren.Collections;
using Siren.Components;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.NativeMenu;
using Siren.Components.Settings;
using Siren.Components.Infrastructure;
using Siren.Components.Variables;
using Siren.History;
using Siren.Infrastructure;
using Siren.Mcp;
using Siren.MockServer;
using Siren.NativeMenu;
using Siren.Variables;
using Velopack;
using Mythetech.Framework.Desktop.Storage.LiteDb;
using LiteDbSettingsStorage = Mythetech.Framework.Desktop.Storage.LiteDb.LiteDbSettingsStorage;

namespace Siren
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Check for --mcp flag for stdio-based MCP server mode (used by Claude Desktop)
            if (args.Contains("--mcp"))
            {
                RunMcpServerAsync(args).GetAwaiter().GetResult();
                return;
            }

            VelopackApp.Build().Run();

            HermesWindow.Prewarm();

            // Register early exception handler to catch startup errors (before window exists)
            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                Console.Error.WriteLine($"Fatal exception: {error.ExceptionObject}");
            };

            var appBuilder = HermesBlazorAppBuilder.CreateDefault(args)
                .WithLicenseKey(HermesLicense.Key);

            appBuilder.ConfigureWindow(options =>
            {
                options.Title = "Siren";
                options.Width = 1920;
                options.Height = 1080;
                options.CenterOnScreen = true;
                options.DevToolsEnabled = true;
                options.CustomTitleBar = true;
                options.WindowStateKey = "";
            });

            // Use AppContext.BaseDirectory for published apps - Directory.GetCurrentDirectory()
            // returns wrong path when launching from shortcuts or Start menu
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            appBuilder.Services
                  .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddConsole();
                });

            appBuilder.Services.AddHttpClient();

            appBuilder.Services.AddDesktopServices(DesktopHost.Hermes);

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

            appBuilder.RootComponents.Add<Components.App>("#app");

            appBuilder.Services.AddSirenComponents<HistoryService, CollectionsService, VariablesService, AppDataService, MockServerService>();

            appBuilder.Services.AddSettingsStorage<LiteDbSettingsStorage>();

            appBuilder.Services.AddSirenMcp();

            appBuilder.Services.AddMessageBus(typeof(App).Assembly);

            appBuilder.Services.AddSettingsFramework();
            appBuilder.Services.RegisterSettingsFromAssemblies(
                typeof(App).Assembly,
                typeof(DesktopHost).Assembly,
                typeof(SettingsBase).Assembly);

            appBuilder.Services.AddSingleton<IAppAsyncInitializer, AppAsyncInitializer>();
            appBuilder.Services.AddJsGuards();

            // Native menu services
            appBuilder.Services.AddSingleton<INativeMenuService, NativeMenuService>();
            appBuilder.Services.AddSingleton<INativeMenuCommandDispatcher, NativeMenuCommandDispatcher>();
            appBuilder.Services.AddSingleton<PluginMenuCallbackRegistry>();

            var app = appBuilder.Build();

            app.Services.UseMessageBus(typeof(App).Assembly);

            app.Services.UseSettingsFramework();

            app.Services.UseSirenMcp();

            app.Services.UseSecretManager();

            app.Services.UsePluginFramework();

            app.RegisterHermesProvider();

            // Initialize native menus
            var menuService = app.Services.GetRequiredService<INativeMenuService>();
            menuService.Initialize(app.MainWindow.MenuBar);

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

            // Settings framework registration (new DI-friendly API)
            services.AddSettingsFramework();
            services.RegisterSettingsFromAssembly(typeof(HttpSettings).Assembly);

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.UseMessageBus();

            serviceProvider.UseSettingsFramework();

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