using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Shouldly;
using Siren.Components;
using Siren.Components.Collections;
using Siren.Components.Http;
using Siren.Components.Services;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class CurlImportDialogTests : TestContext
{
    private const string CurlCommand =
        """curl -X POST 'https://api.example.com/users' -H 'Accept: application/json' -d '{"name":"Ada"}'""";

    private readonly RequestTabsState _tabs;

    public CurlImportDialogTests()
    {
        _tabs = this.AddRequestTabs();
        Services.AddSingleton<ICurlImporter, CurlImporter>();
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private ISnackbar Snackbar => Services.GetRequiredService<ISnackbar>();

    private async Task<IRenderedComponent<MudDialogProvider>> OpenWithPreviewAsync()
    {
        var provider = await this.OpenDialogAsync<CurlImportDialog>("Import from cURL");

        provider.Find("textarea").Input(CurlCommand);
        await provider.ClickButtonAsync("Preview");

        return provider;
    }

    [Fact(DisplayName = "Importing opens the parsed request in a new tab")]
    public async Task Import_OpensRequestInNewTab()
    {
        var provider = await OpenWithPreviewAsync();

        await provider.ClickButtonAsync("Import");

        _tabs.Tabs.Count.ShouldBe(2);
        var request = _tabs.Tabs[^1].Request.ShouldNotBeNull();
        request.RequestUri.ShouldBe("https://api.example.com/users");
        request.Method.ShouldBe(HttpMethod.Post);
        request.RawBody.ShouldBe("""{"name":"Ada"}""");
    }

    [Fact(DisplayName = "Importing makes the new tab the active one")]
    public async Task Import_ActivatesNewTab()
    {
        var provider = await OpenWithPreviewAsync();

        await provider.ClickButtonAsync("Import");

        _tabs.Active.ShouldBeSameAs(_tabs.Tabs[^1]);
        _tabs.Active.Request.ShouldNotBeNull().RequestUri.ShouldBe("https://api.example.com/users");
    }

    [Fact(DisplayName = "Importing closes the dialog")]
    public async Task Import_ClosesDialog()
    {
        var provider = await OpenWithPreviewAsync();

        await provider.ClickButtonAsync("Import");

        provider.HasOpenDialog().ShouldBeFalse();
    }

    [Fact(DisplayName = "Importing reports that the command was imported")]
    public async Task Import_ReportsSuccess()
    {
        var provider = await OpenWithPreviewAsync();

        await provider.ClickButtonAsync("Import");

        Snackbar.ShownSnackbars.ShouldHaveSingleItem().Severity.ShouldBe(Severity.Success);
    }

    [Fact(DisplayName = "Cancelling opens no tab")]
    public async Task Cancel_OpensNoTab()
    {
        var provider = await OpenWithPreviewAsync();

        await provider.ClickButtonAsync("Cancel");

        _tabs.Tabs.ShouldHaveSingleItem();
        Snackbar.ShownSnackbars.ShouldBeEmpty();
    }
}
