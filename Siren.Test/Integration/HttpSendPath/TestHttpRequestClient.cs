using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Siren.Components.History;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Settings;
using Siren.Test.Infrastructure;

namespace Siren.Test.Integration.HttpSendPath;

internal static class TestHttpRequestClient
{
    /// <summary>
    /// Builds the real client with an empty cookie jar and no authentication, over default settings
    /// unless the test supplies its own.
    /// </summary>
    public static HttpRequestClient Create(SettingsState? settings = null)
    {
        return new HttpRequestClient(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IHistoryService>(),
            new NullLogger<HttpRequestClient>(),
            settings ?? TestState.Settings(),
            new CookieService(),
            new RequestAuthenticationState());
    }
}
