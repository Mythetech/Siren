using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Environment;
using Mythetech.Framework.Desktop.Photino;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Photino.Blazor;
using Siren.Collections;
using Siren.Components;
using Siren.History;
using Siren.Infrastructure;
using Siren.Mcp;
using Siren.Variables;
using Velopack;

namespace Siren
{
    public class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            // Check for MCP server mode before starting the GUI app
            if (await McpRegistrationExtensions.TryRunMcpServerAsync(
                args,
                options =>
                {
                    options.ServerName = "Siren";
                },
                services =>
                {
                    services.AddHttpClient();
                    services.AddSirenComponents<HistoryService, CollectionsService, VariablesService, SettingsService>();
                    services.AddSirenMcp();
                },
                typeof(McpServiceExtensions).Assembly))
            {
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

            appBuilder.Services.AddPluginFramework();
            
            appBuilder.Services.AddRuntimeEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) ?? false ? DesktopRuntimeEnvironment.Production() : DesktopRuntimeEnvironment.Development()); 

            // register root component and selector
            appBuilder.RootComponents.Add<Components.App>("app");

            appBuilder.Services.AddSirenComponents<HistoryService, CollectionsService, VariablesService, SettingsService>();

            appBuilder.Services.AddSirenMcp();

            appBuilder.Services.AddMessageBus(typeof(App).Assembly);

            var app = appBuilder.Build();

            app.Services.UseMessageBus(typeof(App).Assembly);

            app.Services.UseSirenMcp();

            app.Services.UsePlugins();

            // customize window
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
    }
}