using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Siren.Components;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.Settings;

namespace Siren.Test.Infrastructure;

internal static class TestState
{
    /// <summary>
    /// Builds a SettingsState over default settings, with the HTTP settings adjusted by the caller.
    /// </summary>
    public static SettingsState Settings(Action<HttpSettings>? configureHttp = null)
    {
        var http = new HttpSettings();
        configureHttp?.Invoke(http);

        var provider = Substitute.For<ISettingsProvider>();
        provider.GetSettings<HttpSettings>().Returns(http);
        provider.GetSettings<ResponseSettings>().Returns(new ResponseSettings());
        provider.GetSettings<HistorySettings>().Returns(new HistorySettings { SaveHttpContent = true });
        provider.GetSettings<EnvironmentSettings>().Returns(new EnvironmentSettings());

        return new SettingsState(provider);
    }

    public static RequestTabsState Tabs(SettingsState? settings = null) => new(settings ?? Settings());

    /// <summary>
    /// The one place tests build a SirenAppState, so its constructor can change without touching every test.
    /// </summary>
    public static SirenAppState AppState(RequestTabsState tabs) => new(tabs);
}
