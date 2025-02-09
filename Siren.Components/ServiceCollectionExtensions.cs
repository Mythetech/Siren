﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Siren.Components.Collections;
using Siren.Components.History;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Settings;
using Siren.Components.Variables;

namespace Siren.Components
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSirenComponents<THistoryService, TCollectionsService, TVariablesService, TSettingsService>(this IServiceCollection services)
            where THistoryService : class, IHistoryService
            where TCollectionsService : class, ICollectionsService
            where TVariablesService : class, IVariableService
            where TSettingsService : class, ISettingsService
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
            });

            services.AddFluentUIComponents();

            services.AddScoped<SirenAppState>();
            services.AddScoped<ICookieService, CookieService>();  
            services.AddSingleton<SettingsState>();

            services.AddHttpClient();

            services.AddTransient<IHttpRequestClient, HttpRequestClient>();

            services.AddSingleton<IHistoryService, THistoryService>();

            services.AddSingleton<ICollectionsService, TCollectionsService>();

            services.AddSingleton<IVariableService, TVariablesService>();

            services.AddSingleton<ISettingsService, TSettingsService>();

            services.AddSingleton<RequestAuthenticationState>();

            return services;
        }
    }
}

