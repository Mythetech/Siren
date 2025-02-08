using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Siren.Collections;
using Siren.Components;
using Siren.History;
using Siren.Infrastructure;
using Siren.Variables;

namespace Siren
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

            appBuilder.Services
                .AddLogging();

            appBuilder.Services.AddHttpClient();

            // register root component and selector
            appBuilder.RootComponents.Add<Components.App>("app");

            appBuilder.Services.AddSirenComponents<HistoryService, CollectionsService, VariablesService, SettingsService>();

            var app = appBuilder.Build();

            // customize window
            app.MainWindow
                .SetSize(1920, 1080)
                .SetUseOsDefaultSize(false)
                .SetFullScreen(false)
                .SetLogVerbosity(10)
                .SetTitle("Siren");

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
            };

            app.Run();
        }
    }
}