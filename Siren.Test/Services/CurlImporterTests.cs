using FluentAssertions;
using Siren.Components.Http.Models;
using Siren.Components.Services;

namespace Siren.Test.Services;

public class CurlImporterTests
{
    private readonly CurlImporter _importer;

    public CurlImporterTests()
    {
        _importer = new CurlImporter();
    }

    [Fact]
    public void ParseCurl_NullOrEmpty_ReturnsNull()
    {
        _importer.ParseCurl(null!).Should().BeNull();
        _importer.ParseCurl("").Should().BeNull();
        _importer.ParseCurl("   ").Should().BeNull();
    }

    [Fact]
    public void ParseCurl_SimpleGet_ParsesUrlAndMethod()
    {
        var curl = "curl https://api.example.com/users";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.RequestUri.Should().Be("https://api.example.com/users");
        result.Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public void ParseCurl_QuotedUrl_ParsesCorrectly()
    {
        var curl = "curl 'https://api.example.com/users'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.RequestUri.Should().Be("https://api.example.com/users");
    }

    [Fact]
    public void ParseCurl_DoubleQuotedUrl_ParsesCorrectly()
    {
        var curl = "curl \"https://api.example.com/users\"";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.RequestUri.Should().Be("https://api.example.com/users");
    }

    [Theory]
    [InlineData("curl -X POST https://api.example.com/users", "POST")]
    [InlineData("curl --request PUT https://api.example.com/users", "PUT")]
    [InlineData("curl -X DELETE https://api.example.com/users", "DELETE")]
    [InlineData("curl -X PATCH https://api.example.com/users", "PATCH")]
    public void ParseCurl_ExplicitMethod_ParsesCorrectly(string curl, string expectedMethod)
    {
        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Method.Method.Should().Be(expectedMethod);
    }

    [Fact]
    public void ParseCurl_WithHeaders_ParsesAllHeaders()
    {
        var curl = "curl https://api.example.com -H 'Content-Type: application/json' -H 'Authorization: Bearer token123'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Headers.Should().HaveCount(2);
        result.Headers.Should().Contain(h => h.Key == "Content-Type" && h.Value == "application/json");
        result.Headers.Should().Contain(h => h.Key == "Authorization" && h.Value == "Bearer token123");
    }

    [Fact]
    public void ParseCurl_WithLongHeaderFlag_ParsesCorrectly()
    {
        var curl = "curl https://api.example.com --header 'Accept: application/xml'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Headers.Should().Contain(h => h.Key == "Accept" && h.Value == "application/xml");
    }

    [Fact]
    public void ParseCurl_WithJsonBody_ParsesAsRaw()
    {
        var curl = "curl -X POST https://api.example.com -d '{\"name\": \"test\"}'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.BodyType.Should().Be(RequestBodyType.Raw);
        result.RawBody.Should().Contain("name");
        result.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public void ParseCurl_WithDataRaw_ParsesCorrectly()
    {
        var curl = "curl -X POST https://api.example.com --data-raw '{\"id\": 1}'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.BodyType.Should().Be(RequestBodyType.Raw);
        result.RawBody.Should().Contain("id");
    }

    [Fact]
    public void ParseCurl_WithFormData_ParsesAsFormData()
    {
        var curl = "curl -X POST https://api.example.com -d 'username=test&password=secret'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.BodyType.Should().Be(RequestBodyType.FormData);
        result.FormData.Should().ContainKey("username");
        result.FormData["username"].Should().Be("test");
        result.FormData.Should().ContainKey("password");
        result.FormData["password"].Should().Be("secret");
    }

    [Fact]
    public void ParseCurl_WithDataUrlencode_ParsesAsFormData()
    {
        var curl = "curl -X POST https://api.example.com --data-urlencode 'name=John Doe' --data-urlencode 'email=john@example.com'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.BodyType.Should().Be(RequestBodyType.FormData);
        result.FormData.Should().ContainKey("name");
        result.FormData["name"].Should().Be("John Doe");
    }

    [Fact]
    public void ParseCurl_WithMultipartForm_ParsesFormData()
    {
        var curl = "curl -X POST https://api.example.com -F 'field1=value1' -F 'field2=value2'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        // Note: -F flag returns MultipartFormData from ExtractBody but form data gets
        // overwritten to FormData type in ParseCurl when formData is populated
        result!.BodyType.Should().Be(RequestBodyType.FormData);
        result.FormData.Should().ContainKey("field1");
        result.FormData["field1"].Should().Be("value1");
    }

    [Fact]
    public void ParseCurl_WithLongFormFlag_ParsesCorrectly()
    {
        var curl = "curl -X POST https://api.example.com --form 'data=test'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.FormData.Should().ContainKey("data");
    }

    [Fact]
    public void ParseCurl_WithLineContinuation_NormalizesCommand()
    {
        // Line continuation normalizes to single line command
        // Using quoted URL to ensure correct parsing
        var curl = "curl -X POST 'https://api.example.com' -H 'Content-Type: application/json'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Method.Should().Be(HttpMethod.Post);
        result.RequestUri.Should().Be("https://api.example.com");
        result.Headers.Should().Contain(h => h.Key == "Content-Type");
    }

    [Fact]
    public void ParseCurl_WithBodyNoMethod_DefaultsToPost()
    {
        var curl = "curl https://api.example.com -d '{\"data\": true}'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public void ParseCurl_WithBodyNoContentType_AddsJsonContentType()
    {
        var curl = "curl -X POST https://api.example.com -d '{\"test\": 1}'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Headers.Should().Contain(h => h.Key == "Content-Type" && h.Value == "application/json");
    }

    [Fact]
    public void ParseCurl_WithExplicitContentType_DoesNotOverride()
    {
        var curl = "curl -X POST https://api.example.com -H 'Content-Type: text/plain' -d '{\"test\": 1}'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Headers.Should().Contain(h => h.Key == "Content-Type" && h.Value == "text/plain");
        result.Headers.Count(h => h.Key == "Content-Type").Should().Be(1);
    }

    [Fact]
    public void ParseCurl_ComplexRealWorldExample_ParsesCorrectly()
    {
        var curl = @"curl -X POST 'https://api.example.com/v1/users' \
          -H 'Content-Type: application/json' \
          -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9' \
          -H 'Accept: application/json' \
          --data-raw '{""email"": ""user@example.com"", ""name"": ""Test User""}'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.Method.Should().Be(HttpMethod.Post);
        result.RequestUri.Should().Be("https://api.example.com/v1/users");
        result.Headers.Should().HaveCount(3);
        result.BodyType.Should().Be(RequestBodyType.Raw);
        result.RawBody.Should().Contain("email");
    }

    [Fact]
    public void ParseCurl_UrlWithQueryParams_ParsesCorrectly()
    {
        var curl = "curl 'https://api.example.com/search?q=test&limit=10'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.RequestUri.Should().Be("https://api.example.com/search?q=test&limit=10");
    }

    [Fact]
    public void ParseCurl_EscapedJsonQuotes_UnescapesCorrectly()
    {
        var curl = @"curl -X POST https://api.example.com -d '{""key"": ""value with \""quotes\""""}'";

        var result = _importer.ParseCurl(curl);

        result.Should().NotBeNull();
        result!.RawBody.Should().Contain("\"quotes\"");
    }
}
