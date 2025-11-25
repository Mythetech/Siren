using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Siren.Components.Http;
using Siren.Components.RequestContextPanel.Authentication;
using Siren.Components.Services;
using Siren.Components.Settings;
using Siren.Components.History;

namespace Siren.Test.Integration;

public class HttpRequestTests
{
    private HttpRequestClient _client;
    private IHttpClientFactory _httpClientFactory;
    private IHistoryService _historyService;
    private ILogger<HttpRequestClient> _logger;
    private SettingsState _settings;
    private ICookieService _cookieService;
    private RequestAuthenticationState _authState;
    
    public HttpRequestTests()
    {
        _httpClientFactory = new MockHttpClientFactory();
        _historyService = Substitute.For<IHistoryService>();
        _logger = new NullLogger<HttpRequestClient>();
        _settings = new SettingsState { SaveHttpContent = true };
        _cookieService = new CookieService();
        _authState = new RequestAuthenticationState();
        
        _client = new HttpRequestClient(
            _httpClientFactory,
            _historyService,
            _logger,
            _settings,
            _cookieService,
            _authState
        );
    }

    private class MockHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }

    [Fact]
    public async Task Can_SendFormData_Request()
    {
        var formData = new Dictionary<string, string>
        {
            { "name", "John Doe" },
            { "email", "john@example.com" },
            { "message", "Hello World" }
        };

        var request = new HttpRequest
        {
            Method = HttpMethod.Post,
            RequestUri = "https://httpbin.org/post",
            ContentType = "application/x-www-form-urlencoded",
            FormData = formData,
            Content = new FormUrlEncodedContent(formData)
        };

        var result = await _client.SendHttpRequestAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Error.Should().BeNull();
        result.StatusCode.Should().Be(200);
        result.HttpStatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseText = result.ResponseText;
        responseText.Should().NotBeNullOrEmpty();
        
        var jsonDoc = JsonDocument.Parse(responseText);
        var form = jsonDoc.RootElement.GetProperty("form");
        
        form.GetProperty("name").GetString().Should().Be("John Doe");
        form.GetProperty("email").GetString().Should().Be("john@example.com");
        form.GetProperty("message").GetString().Should().Be("Hello World");
    }

    [Fact]
    public async Task Can_SendFormData_WithSpecialCharacters()
    {
        var formData = new Dictionary<string, string>
        {
            { "key1", "value with spaces" },
            { "key2", "value&with=special&chars" },
            { "key3", "value+with+plus" }
        };

        var request = new HttpRequest
        {
            Method = HttpMethod.Post,
            RequestUri = "https://httpbin.org/post",
            ContentType = "application/x-www-form-urlencoded",
            FormData = formData,
            Content = new FormUrlEncodedContent(formData)
        };

        var result = await _client.SendHttpRequestAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Error.Should().BeNull();
        result.StatusCode.Should().Be(200);
        
        var responseText = result.ResponseText;
        var jsonDoc = JsonDocument.Parse(responseText);
        var form = jsonDoc.RootElement.GetProperty("form");
        
        form.GetProperty("key1").GetString().Should().Be("value with spaces");
        form.GetProperty("key2").GetString().Should().Be("value&with=special&chars");
        form.GetProperty("key3").GetString().Should().Be("value+with+plus");
    }

    [Fact]
    public async Task Can_SendFormData_WithEmptyValues()
    {
        var formData = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "" },
            { "key3", "value3" }
        };

        var request = new HttpRequest
        {
            Method = HttpMethod.Post,
            RequestUri = "https://httpbin.org/post",
            ContentType = "application/x-www-form-urlencoded",
            FormData = formData,
            Content = new FormUrlEncodedContent(formData)
        };

        var result = await _client.SendHttpRequestAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Error.Should().BeNull();
        result.StatusCode.Should().Be(200);
        
        var responseText = result.ResponseText;
        var jsonDoc = JsonDocument.Parse(responseText);
        var form = jsonDoc.RootElement.GetProperty("form");
        
        form.GetProperty("key1").GetString().Should().Be("value1");
        form.GetProperty("key2").GetString().Should().Be("");
        form.GetProperty("key3").GetString().Should().Be("value3");
    }
}
