using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Settings;

namespace Siren.Test.Integration.HttpSendPath;

internal static class TestHttpRequestClient
{
    /// <summary>
    /// Builds the real client with default settings, an empty cookie jar and no authentication.
    /// </summary>
    public static HttpRequestClient Create()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.GetSettings<HttpSettings>().Returns(new HttpSettings());
        settingsProvider.GetSettings<ResponseSettings>().Returns(new ResponseSettings());
        settingsProvider.GetSettings<HistorySettings>().Returns(new HistorySettings { SaveHttpContent = true });
        settingsProvider.GetSettings<EnvironmentSettings>().Returns(new EnvironmentSettings());

        return new HttpRequestClient(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IHistoryService>(),
            new NullLogger<HttpRequestClient>(),
            new SettingsState(settingsProvider),
            new CookieService(),
            new RequestAuthenticationState());
    }
}
