using NSubstitute;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Components.Services;
using Siren.Components.Variables;
using Siren.Test.Infrastructure;

namespace Siren.Test.Services;

public class RequestComposerTests
{
    private const string Dev = "Dev";

    private readonly List<Variable> _variables = [];
    private readonly RequestComposer _composer;

    public RequestComposerTests()
    {
        var variableService = Substitute.For<IVariableService>();
        variableService.GetVariables().Returns(_ => _variables.ToList());

        _composer = new RequestComposer(new VariableSubstitutionService(variableService, [new FailingVariableResolver()]));
    }

    private void Define(string key, string value) => _variables.Add(Variable.Create(key, value, group: Dev));

    private Task<ComposedRequest> ComposeAsync(HttpRequest template) => _composer.ComposeAsync(template, Dev);

    private static BinaryAttachment File(string name, string contentType, params byte[] data) => new()
    {
        FileName = name,
        ContentType = contentType,
        Data = data,
    };

    private static string Unquoted(string? value) => value.ShouldNotBeNull().Trim('"');

    [Fact(DisplayName = "Variables are substituted in the URL")]
    public async Task Url_IsSubstituted()
    {
        Define("host", "api.example.com");

        var composed = await ComposeAsync(new HttpRequest { RequestUri = "https://{{host}}/users", BodyType = RequestBodyType.None });

        composed.Request.RequestUri.ShouldBe("https://api.example.com/users");
        composed.ResolutionErrors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Variables are substituted in header names and values")]
    public async Task Headers_AreSubstituted()
    {
        Define("header", "X-Tenant");
        Define("tenant", "acme");

        var composed = await ComposeAsync(new HttpRequest
        {
            BodyType = RequestBodyType.None,
            Headers = [new("{{header}}", "{{tenant}}")],
        });

        composed.Request.Headers.ShouldBe([new KeyValuePair<string, string>("X-Tenant", "acme")]);
    }

    [Fact(DisplayName = "Variables are substituted in the raw body")]
    public async Task RawBody_IsSubstituted()
    {
        Define("user", "Ada");

        var composed = await ComposeAsync(new HttpRequest { BodyType = RequestBodyType.Raw, RawBody = """{"name":"{{user}}"}""" });

        (await composed.Request.Content.ReadAsStringAsync()).ShouldBe("""{"name":"Ada"}""");
    }

    [Fact(DisplayName = "Variables are substituted in form field names and values")]
    public async Task FormFields_AreSubstituted()
    {
        Define("field", "name");
        Define("user", "Ada Lovelace");

        var composed = await ComposeAsync(new HttpRequest
        {
            BodyType = RequestBodyType.FormData,
            FormData = new Dictionary<string, string> { ["{{field}}"] = "{{user}}" },
        });

        composed.Request.Content.ShouldBeOfType<FormUrlEncodedContent>();
        (await composed.Request.Content.ReadAsStringAsync()).ShouldBe("name=Ada+Lovelace");
        composed.Request.ContentType.ShouldBe("application/x-www-form-urlencoded");
    }

    [Fact(DisplayName = "A request with no body has no content")]
    public async Task NoBody_HasNoContent()
    {
        var composed = await ComposeAsync(new HttpRequest { BodyType = RequestBodyType.None, RawBody = "ignored" });

        composed.Request.Content.ShouldBeNull();
    }

    [Fact(DisplayName = "A raw body is sent with the content type of the Content-Type header row")]
    public async Task RawBody_UsesContentTypeHeaderRow()
    {
        var composed = await ComposeAsync(new HttpRequest
        {
            BodyType = RequestBodyType.Raw,
            RawBody = "<note/>",
            Headers = [new("content-type", "text/xml")],
        });

        composed.Request.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/xml");
        composed.Request.ContentType.ShouldBe("text/xml");
    }

    [Fact(DisplayName = "A raw body without a Content-Type row is sent as JSON")]
    public async Task RawBody_DefaultsToJson()
    {
        var composed = await ComposeAsync(new HttpRequest { BodyType = RequestBodyType.Raw, RawBody = "{}" });

        composed.Request.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
    }

    [Fact(DisplayName = "Multipart form data carries the fields and then the files")]
    public async Task Multipart_CarriesFieldsThenFiles()
    {
        var composed = await ComposeAsync(new HttpRequest
        {
            BodyType = RequestBodyType.MultipartFormData,
            FormData = new Dictionary<string, string> { ["name"] = "Ada" },
            BinaryAttachments = [File("avatar.png", "image/png", 1, 2, 3)],
        });

        var parts = composed.Request.Content.ShouldBeOfType<MultipartFormDataContent>().ToList();
        parts.Count.ShouldBe(2);
        Unquoted(parts[0].Headers.ContentDisposition.ShouldNotBeNull().Name).ShouldBe("name");
        (await parts[0].ReadAsStringAsync()).ShouldBe("Ada");
        Unquoted(parts[1].Headers.ContentDisposition.ShouldNotBeNull().Name).ShouldBe("file");
        Unquoted(parts[1].Headers.ContentDisposition.ShouldNotBeNull().FileName).ShouldBe("avatar.png");
        parts[1].Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("image/png");
        (await parts[1].ReadAsByteArrayAsync()).ShouldBe([1, 2, 3]);
    }

    [Fact(DisplayName = "One binary attachment is sent as the body with its content type")]
    public async Task Binary_OneAttachment_IsTheBody()
    {
        var composed = await ComposeAsync(new HttpRequest
        {
            BodyType = RequestBodyType.Binary,
            BinaryAttachments = [File("avatar.png", "image/png", 1, 2, 3)],
        });

        var content = composed.Request.Content.ShouldBeOfType<ByteArrayContent>();
        content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("image/png");
        (await content.ReadAsByteArrayAsync()).ShouldBe([1, 2, 3]);
    }

    [Fact(DisplayName = "Several binary attachments are sent as multipart files")]
    public async Task Binary_SeveralAttachments_AreMultipart()
    {
        var composed = await ComposeAsync(new HttpRequest
        {
            BodyType = RequestBodyType.Binary,
            BinaryAttachments = [File("a.png", "image/png", 1), File("b.txt", "text/plain", 2)],
        });

        var parts = composed.Request.Content.ShouldBeOfType<MultipartFormDataContent>().ToList();
        parts.Select(p => Unquoted(p.Headers.ContentDisposition.ShouldNotBeNull().FileName)).ShouldBe(["a.png", "b.txt"]);
    }

    [Fact(DisplayName = "A binary body with no attachment has no content")]
    public async Task Binary_NoAttachment_HasNoContent()
    {
        var composed = await ComposeAsync(new HttpRequest { BodyType = RequestBodyType.Binary });

        composed.Request.Content.ShouldBeNull();
    }

    [Fact(DisplayName = "A variable that cannot be resolved is reported once")]
    public async Task ResolutionErrors_AreCollectedOnce()
    {
        Define("token", FailingVariableResolver.Value);

        var composed = await ComposeAsync(new HttpRequest
        {
            BodyType = RequestBodyType.None,
            RequestUri = "https://api.example.com/users",
            Headers = [new("Authorization", "{{token}}"), new("X-Again", "{{token}}")],
        });

        composed.ResolutionErrors.ShouldBe([FailingVariableResolver.Error]);
    }

    [Fact(DisplayName = "The template is left as it was")]
    public async Task Template_IsNotChanged()
    {
        Define("host", "api.example.com");
        var template = new HttpRequest
        {
            RequestUri = "https://{{host}}/users",
            Method = HttpMethod.Post,
            BodyType = RequestBodyType.Raw,
            RawBody = """{"host":"{{host}}"}""",
            Headers = [new("X-Host", "{{host}}")],
            FormData = new Dictionary<string, string> { ["host"] = "{{host}}" },
        };

        var composed = await ComposeAsync(template);

        composed.Request.ShouldNotBeSameAs(template);
        composed.Request.Headers.ShouldNotBeSameAs(template.Headers);
        composed.Request.FormData.ShouldNotBeSameAs(template.FormData);
        template.RequestUri.ShouldBe("https://{{host}}/users");
        template.RawBody.ShouldBe("""{"host":"{{host}}"}""");
        template.Headers.ShouldBe([new KeyValuePair<string, string>("X-Host", "{{host}}")]);
        template.Content.ShouldBeNull();
    }

    [Fact(DisplayName = "The composed request keeps the method, body type and unresolved raw body")]
    public async Task ComposedRequest_KeepsWhatHistoryStores()
    {
        Define("user", "Ada");
        var template = new HttpRequest
        {
            Method = HttpMethod.Put,
            BodyType = RequestBodyType.Raw,
            RawBody = """{"name":"{{user}}"}""",
            FormData = new Dictionary<string, string> { ["kept"] = "{{user}}" },
        };

        var composed = await ComposeAsync(template);

        composed.Request.Method.ShouldBe(HttpMethod.Put);
        composed.Request.BodyType.ShouldBe(RequestBodyType.Raw);
        composed.Request.RawBody.ShouldBe("""{"name":"{{user}}"}""");
        composed.Request.FormData.ShouldBe(new Dictionary<string, string> { ["kept"] = "{{user}}" });
    }

    [Fact(DisplayName = "A request without a method is composed as GET")]
    public async Task NoMethod_IsComposedAsGet()
    {
        var composed = await ComposeAsync(new HttpRequest { BodyType = RequestBodyType.None });

        composed.Request.Method.ShouldBe(HttpMethod.Get);
    }
}
