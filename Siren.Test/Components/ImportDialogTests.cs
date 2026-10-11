using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Shouldly;
using Siren.Components;
using Siren.Components.Collections;
using Siren.Components.Http.Models;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class ImportDialogTests : TestContext
{
    private const string SpecUrl = "https://petstore.example/openapi.json";

    private readonly FakeCollectionsService _collections = new()
    {
        OpenApiImportResult = Collection.Create("petstore.example/openapi.json",
        [
            new HttpRequest { Method = HttpMethod.Get, RequestUri = "https://petstore.example/pets", DisplayUri = "/pets" }
        ])
    };

    public ImportDialogTests()
    {
        Services.AddSingleton<ICollectionsService>(_collections);
        Services.AddSingleton<SirenAppState>();
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private ISnackbar Snackbar => Services.GetRequiredService<ISnackbar>();

    private async Task<IRenderedComponent<MudDialogProvider>> OpenWithImportedSpecAsync()
    {
        var provider = await this.OpenDialogAsync<ImportDialog>("Open API Import");

        provider.Find("input").Change(SpecUrl);
        await provider.ClickButtonAsync("Import");
        provider.WaitForAssertion(() => provider.FindButton("Save").ShouldNotBeNull());

        return provider;
    }

    [Fact(DisplayName = "Saving creates the imported collection")]
    public async Task Save_CreatesCollection()
    {
        var provider = await OpenWithImportedSpecAsync();

        await provider.ClickButtonAsync("Save");

        var created = _collections.GetCollections().ShouldHaveSingleItem();
        created.Name.ShouldBe("petstore.example/openapi.json");
        created.Requests.ShouldHaveSingleItem().RequestUri.ShouldBe("https://petstore.example/pets");
    }

    [Fact(DisplayName = "Saving closes the dialog")]
    public async Task Save_ClosesDialog()
    {
        var provider = await OpenWithImportedSpecAsync();

        await provider.ClickButtonAsync("Save");

        provider.HasOpenDialog().ShouldBeFalse();
    }

    [Fact(DisplayName = "Saving reports that the collection was imported")]
    public async Task Save_ReportsSuccess()
    {
        var provider = await OpenWithImportedSpecAsync();

        await provider.ClickButtonAsync("Save");

        Snackbar.ShownSnackbars.ShouldHaveSingleItem().Severity.ShouldBe(Severity.Success);
    }

    [Fact(DisplayName = "Cancelling creates nothing")]
    public async Task Cancel_CreatesNothing()
    {
        var provider = await OpenWithImportedSpecAsync();

        await provider.ClickButtonAsync("Cancel");

        _collections.GetCollections().ShouldBeEmpty();
        Snackbar.ShownSnackbars.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A failed save is reported as an error")]
    public async Task Save_WhenCreateFails_ReportsError()
    {
        _collections.CreateFailure = new InvalidOperationException("database is locked");
        var provider = await OpenWithImportedSpecAsync();

        await provider.ClickButtonAsync("Save");

        Snackbar.ShownSnackbars.ShouldHaveSingleItem().Severity.ShouldBe(Severity.Error);
        provider.HasOpenDialog().ShouldBeFalse();
    }
}
