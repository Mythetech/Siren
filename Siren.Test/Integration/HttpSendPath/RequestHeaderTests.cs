using System.Text;
using System.Text.Json;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Components.Services;

namespace Siren.Test.Integration.HttpSendPath;

public class RequestHeaderTests : IClassFixture<EchoHostFixture>
{
    private readonly EchoHostFixture _host;

    public RequestHeaderTests(EchoHostFixture host)
    {
        _host = host;
    }

    private async Task<JsonElement> SendAndReadEchoAsync(HttpRequest request)
    {
        request.RequestUri = $"{_host.BaseUrl}/echo";

        var result = await TestHttpRequestClient.Create().SendHttpRequestAsync(request, CancellationToken.None);

        result.Error.ShouldBeNull();
        result.StatusCode.ShouldBe(200);

        var body = await result.ResponseContent!.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static List<KeyValuePair<string, string>> Headers(string name, string value) => [new(name, value)];

    [Fact(DisplayName = "A Content-Type header row sets the content type of the body")]
    public async Task ContentTypeHeader_SetsBodyContentType()
    {
        var echo = await SendAndReadEchoAsync(new HttpRequest
        {
            Method = HttpMethod.Post,
            Headers = Headers("Content-Type", "text/xml"),
            Content = new StringContent("<note/>", Encoding.UTF8, "application/json"),
        });

        echo.GetProperty("contentType").GetString().ShouldBe("text/xml");
        echo.GetProperty("body").GetString().ShouldBe("<note/>");
    }

    [Fact(DisplayName = "A Content-Type header row with parameters is sent as typed")]
    public async Task ContentTypeHeader_WithParameters_IsSentAsTyped()
    {
        var echo = await SendAndReadEchoAsync(new HttpRequest
        {
            Method = HttpMethod.Post,
            Headers = Headers("Content-Type", "application/vnd.api+json; charset=utf-8"),
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        });

        echo.GetProperty("contentType").GetString().ShouldBe("application/vnd.api+json; charset=utf-8");
    }

    [Fact(DisplayName = "A Content-Type header row is sent even when the request has no body")]
    public async Task ContentTypeHeader_WithoutBody_IsSent()
    {
        var echo = await SendAndReadEchoAsync(new HttpRequest
        {
            Method = HttpMethod.Get,
            Headers = Headers("Content-Type", "application/json"),
            Content = null!,
        });

        echo.GetProperty("contentType").GetString().ShouldBe("application/json");
    }

    [Fact(DisplayName = "A Content-Type header row does not replace a multipart boundary")]
    public async Task ContentTypeHeader_OnMultipartBody_KeepsBoundary()
    {
        var multipart = new MultipartFormDataContent { { new StringContent("Ada"), "name" } };
        var generatedContentType = multipart.Headers.ContentType!.ToString();

        var echo = await SendAndReadEchoAsync(new HttpRequest
        {
            Method = HttpMethod.Post,
            Headers = Headers("Content-Type", "multipart/form-data; boundary=copied-from-somewhere-else"),
            Content = multipart,
        });

        echo.GetProperty("contentType").GetString().ShouldBe(generatedContentType);
    }

    [Fact(DisplayName = "An imported cURL command with a JSON body reaches the server intact")]
    public async Task ImportedCurlCommand_ReachesServerIntact()
    {
        var imported = new CurlImporter().ParseCurl(
            """curl -X POST 'https://api.example.com/users' -H 'Content-Type: application/json; charset=utf-8' -d '{"name":"Ada"}'""")!;

        // The page builds a raw body from the editor text and the Content-Type header row before sending
        var contentType = imported.Headers.Single(h => h.Key == "Content-Type").Value;
        imported.Content = RawBodyContent.Create(imported.RawBody, contentType);

        var echo = await SendAndReadEchoAsync(imported);

        echo.GetProperty("method").GetString().ShouldBe("POST");
        echo.GetProperty("contentType").GetString().ShouldBe("application/json; charset=utf-8");
        echo.GetProperty("body").GetString().ShouldBe("""{"name":"Ada"}""");
    }

    [Fact(DisplayName = "A header value that fails strict validation is sent as typed")]
    public async Task LooselyFormattedHeaderValue_IsSentAsTyped()
    {
        var echo = await SendAndReadEchoAsync(new HttpRequest
        {
            Method = HttpMethod.Get,
            Headers = Headers("If-None-Match", "abc123"),
            Content = null!,
        });

        echo.GetProperty("headers").GetProperty("If-None-Match").GetString().ShouldBe("abc123");
    }
}
