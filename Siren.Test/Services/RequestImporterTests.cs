using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Commands;
using Siren.Components.Http.Consumers;
using Siren.Components.Http.Models;
using Siren.Test.Infrastructure;

namespace Siren.Test.Services;

public sealed class RequestImporterTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"siren-import-{Guid.NewGuid():N}.json");
    private readonly RequestTabsState _tabs = TestState.Tabs();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly RequestImporter _importer;

    public RequestImporterTests()
    {
        _importer = new RequestImporter(
            Substitute.For<IFileOpenService>(),
            _bus,
            NullLogger<RequestImporter>.Instance,
            _tabs);
    }

    public void Dispose() => File.Delete(_file);

    private void WriteRequestFile(string bodyType, string? body)
    {
        var saved = new SaveRequest(
            Method: "POST",
            RequestUri: "https://api.example.com/users",
            DisplayUri: "https://api.example.com/users",
            QueryParameters: null,
            Headers: [new("Accept", "application/json")],
            FormData: new Dictionary<string, string>(),
            ContentType: "application/json",
            Body: body,
            BodyType: bodyType,
            Timeout: TimeSpan.FromSeconds(100),
            RetryAttempts: 3,
            Id: Guid.NewGuid());

        File.WriteAllText(_file, JsonSerializer.Serialize(saved));
    }

    [Fact(DisplayName = "An imported request opens in a new active tab with its body")]
    public async Task Import_OpensRequestWithBody()
    {
        WriteRequestFile("raw", """{"name":"Ada"}""");

        await _importer.Consume(new ImportRequest(_file));

        _tabs.Tabs.Count.ShouldBe(2);
        var request = _tabs.Active.Request.ShouldNotBeNull();
        request.RequestUri.ShouldBe("https://api.example.com/users");
        request.Method.ShouldBe(HttpMethod.Post);
        request.BodyType.ShouldBe(RequestBodyType.Raw);
        request.RawBody.ShouldBe("""{"name":"Ada"}""");
        request.Headers.ShouldBe([new KeyValuePair<string, string>("Accept", "application/json")]);
        await _bus.Received(1).PublishAsync(new AddNotification("Request imported successfully", Severity.Success));
    }

    [Fact(DisplayName = "An imported form request keeps its body type")]
    public async Task Import_FormRequest_KeepsBodyType()
    {
        WriteRequestFile("form-data", null);

        await _importer.Consume(new ImportRequest(_file));

        _tabs.Active.Request.ShouldNotBeNull().BodyType.ShouldBe(RequestBodyType.FormData);
    }

    [Fact(DisplayName = "A file that is not a request reports an error and opens no tab")]
    public async Task Import_InvalidFile_OpensNoTab()
    {
        File.WriteAllText(_file, "not a request");

        await _importer.Consume(new ImportRequest(_file));

        _tabs.Tabs.ShouldHaveSingleItem();
        await _bus.Received(1).PublishAsync(new AddNotification("Invalid request file format", Severity.Error));
    }

    [Fact(DisplayName = "A file that does not exist opens no tab")]
    public async Task Import_MissingFile_OpensNoTab()
    {
        await _importer.Consume(new ImportRequest(_file));

        _tabs.Tabs.ShouldHaveSingleItem();
        await _bus.DidNotReceive().PublishAsync(Arg.Any<AddNotification>());
    }
}
