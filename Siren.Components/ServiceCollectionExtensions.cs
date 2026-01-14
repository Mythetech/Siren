using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.Collections;
using Siren.Components.Configuration;
using Siren.Components.History;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Settings;
using Siren.Components.Variables;

namespace Siren.Components;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSirenComponents<THistoryService, TCollectionsService, TVariablesService, TAppDataService>(this IServiceCollection services)
        where THistoryService : class, IHistoryService
        where TCollectionsService : class, ICollectionsService
        where TVariablesService : class, IVariableService
        where TAppDataService : class, IAppDataService
    {
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
            config.SnackbarConfiguration.HideTransitionDuration = 200;
            config.SnackbarConfiguration.ShowTransitionDuration = 100;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
            config.PopoverOptions.QueueDelay = TimeSpan.FromSeconds(0.01);
        });

        services.AddFluentUIComponents();

        services.AddSingleton<SirenAppState>();
        services.AddSingleton<ICookieService, CookieService>();

        // Settings framework
        services.AddSettingsFramework();
        services.AddSingleton<IAppDataService, TAppDataService>();

        // SettingsState adapter for backward compatibility
        services.AddSingleton<SettingsState>(sp =>
        {
            var settingsProvider = sp.GetRequiredService<ISettingsProvider>();
            return new SettingsState(settingsProvider);
        });
        services.AddSingleton<VariableState>();

        services.AddHttpClient();

        services.AddTransient<IHttpRequestClient, HttpRequestClient>();

        services.AddSingleton<IHistoryService, THistoryService>();

        services.AddSingleton<ICollectionsService, TCollectionsService>();

        services.AddSingleton<IVariableService, TVariablesService>();

        // Variable value resolvers - order matters (more specific resolvers first)
        services.AddSingleton<IVariableValueResolver, DynamicVariableResolver>();

        // Secret manager framework (SecretManagerState will be registered by desktop project)
        services.AddSecretManagerFramework();

        services.AddSingleton<IVariableSubstitutionService, VariableSubstitutionService>();

        services.AddSingleton<RequestAuthenticationState>();

        services.AddSingleton<AppConfiguration>();

        services.AddSingleton<ICurlImporter, CurlImporter>();
        services.AddSingleton<ICurlGenerator, CurlGenerator>();
        services.AddSingleton<IHarExporter, HarExporter>();

        return services;
    }
}

