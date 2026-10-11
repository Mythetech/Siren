using Shouldly;
using Siren.Components.Http;

namespace Siren.Test.Services;

public class RawBodyContentTests
{
    private static string ContentTypeOf(HttpContent content)
        => content.Headers.NonValidated["Content-Type"].ToString();

    [Fact(DisplayName = "A bare media type gets the UTF-8 charset the body is encoded with")]
    public void BareMediaType_GetsUtf8Charset()
    {
        var content = RawBodyContent.Create("{}", "application/json");

        ContentTypeOf(content).ShouldBe("application/json; charset=utf-8");
    }

    [Fact(DisplayName = "A content type with parameters is kept as typed")]
    public void ContentTypeWithParameters_IsKeptAsTyped()
    {
        var content = RawBodyContent.Create("{}", "application/vnd.api+json; charset=utf-8; profile=\"https://example.com\"");

        ContentTypeOf(content).ShouldBe("application/vnd.api+json; charset=utf-8; profile=\"https://example.com\"");
    }

    [Fact(DisplayName = "A content type that is not a valid media type is kept as typed")]
    public void InvalidMediaType_IsKeptAsTyped()
    {
        var content = RawBodyContent.Create("payload", "not a media type");

        ContentTypeOf(content).ShouldBe("not a media type");
    }

    [Theory(DisplayName = "A blank content type falls back to JSON")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BlankContentType_FallsBackToJson(string? contentType)
    {
        var content = RawBodyContent.Create("{}", contentType);

        ContentTypeOf(content).ShouldBe("application/json; charset=utf-8");
    }

    [Fact(DisplayName = "The body is sent as typed")]
    public async Task Payload_IsPreserved()
    {
        var content = RawBodyContent.Create("{\"name\":\"Ada\"}", "application/json; charset=utf-8");

        (await content.ReadAsStringAsync()).ShouldBe("{\"name\":\"Ada\"}");
    }
}
