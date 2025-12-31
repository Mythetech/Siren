using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Photino;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Photino.Blazor;
using Siren.Collections;
using Siren.Components;
using Siren.History;
using Siren.Infrastructure;
using Siren.Variables;
using Velopack;

namespace Siren
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            VelopackApp.Build().Run();

            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services
                .AddLogging();

            appBuilder.Services.AddHttpClient();

            appBuilder.Services.AddDesktopServices();

            appBuilder.Services.AddPluginFramework();

            // register root component and selector
            appBuilder.RootComponents.Add<Components.App>("app");

            appBuilder.Services.AddSirenComponents<HistoryService, CollectionsService, VariablesService, SettingsService>();

            appBuilder.Services.AddMessageBus(typeof(App).Assembly);

            var app = appBuilder.Build();

            app.Services.UseMessageBus(typeof(App).Assembly);
            
            var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");

            app.Services.UsePlugins(pluginDir);

            // customize window
            app.MainWindow
                .SetSize(1920, 1080)
                .SetUseOsDefaultSize(false)
                .SetFullScreen(false)
                .SetLogVerbosity(10)
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