using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Settings;

namespace Siren.Test.Integration.HttpSendPath;

public class HttpVerbRoundTripTests : IClassFixture<EchoHostFixture>
{
    private readonly EchoHostFixture _host;

    public HttpVerbRoundTripTests(EchoHostFixture host)
    {
        _host = host;
    }

    public static TheoryData<string, bool> Verbs => new()
    {
        { "GET", false },
        { "POST", true },
        { "PUT", true },
        { "DELETE", false },
        { "PATCH", true },
        { "OPTIONS", false },
        { "HEAD", false },
        { "QUERY", true },
    };

    [Theory]
    [MemberData(nameof(Verbs))]
    public async Task SendHttpRequest_Puts_Verb_Headers_And_Body_OnTheWire(string verb, bool sendsBody)
    {
        const string bodyJson = """{"hello":"world"}""";

        var request = new HttpRequest
        {
            Method = new HttpMethod(verb),
            RequestUri = $"{_host.BaseUrl}/echo",
            Headers = new List<KeyValuePair<string, string>>
            {
                new("X-Siren-Test", "hello"),
            },
            Content = sendsBody
                ? new StringContent(bodyJson, Encoding.UTF8, "application/json")
                : null!,
            ContentType = "application/json",
        };

        var client = BuildClient();
        var result = await client.SendHttpRequestAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Error.Should().BeNull();
        result.StatusCode.Should().Be(200);

        result.Headers.Should().ContainKey("X-Echo-Method");
        result.Headers["X-Echo-Method"].Should().Be(verb);

        var responseBody = await result.ResponseContent!.ReadAsStringAsync();

        if (verb == "HEAD")
        {
            responseBody.Should().BeEmpty();
            return;
        }

        using var json = JsonDocument.Parse(responseBody);
        json.RootElement.GetProperty("method").GetString().Should().Be(verb);

        var headers = json.RootElement.GetProperty("headers");
        headers.TryGetProperty("X-Siren-Test", out var sirenHeader).Should().BeTrue();
        sirenHeader.GetString().Should().Be("hello");

        if (sendsBody)
        {
            json.RootElement.GetProperty("body").GetString().Should().Be(bodyJson);
            json.RootElement.GetProperty("contentType").GetString().Should().Contain("application/json");
        }
    }

    private static HttpRequestClient BuildClient()
    {
        var historyService = Substitute.For<IHistoryService>();
        var factory = Substitute.For<IHttpClientFactory>();

        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.GetSettings<HttpSettings>().Returns(new HttpSettings());
        settingsProvider.GetSettings<ResponseSettings>().Returns(new ResponseSettings());
        settingsProvider.GetSettings<HistorySettings>().Returns(new HistorySettings { SaveHttpContent = true });
        settingsProvider.GetSettings<EnvironmentSettings>().Returns(new EnvironmentSettings());

        var settings = new SettingsState(settingsProvider);
        var cookieService = new CookieService();
        var authState = new RequestAuthenticationState();

        return new HttpRequestClient(
            factory,
            historyService,
            new NullLogger<HttpRequestClient>(),
            settings,
            cookieService,
            authState);
    }
}
