using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Shouldly;
using Siren.Components;
using Siren.Components.Collections;
using Siren.Components.Http.Models;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class CollectionsPanelTests : TestContext
{
    private readonly FakeCollectionsService _collections = new();

    public CollectionsPanelTests()
    {
        Services.AddSingleton<ICollectionsService>(_collections);
        Services.AddSingleton<SirenAppState>();
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    private IRenderedComponent<CollectionsPanel> RenderLoadedPanel()
    {
        var cut = RenderComponent<CollectionsPanel>();
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Loading collections"));
        return cut;
    }

    [Fact(DisplayName = "Adding the first collection shows it")]
    public async Task AddCollection_FromEmptyState_ShowsCollection()
    {
        var cut = RenderLoadedPanel();

        await cut.ClickButtonAsync("Add Collection");

        cut.WaitForAssertion(() => cut.FindComponents<CollectionCard>().Count.ShouldBe(1));
    }

    [Fact(DisplayName = "A collection created elsewhere appears in the open panel")]
    public void CollectionCreatedElsewhere_ShowsCollection()
    {
        var cut = RenderLoadedPanel();

        _collections.CreateCollection("From the menu", null);

        cut.WaitForAssertion(() => cut.FindComponents<CollectionCard>().Count.ShouldBe(1));
    }

    [Fact(DisplayName = "Importing an OpenAPI spec from the panel adds one collection")]
    public async Task OpenApiImport_Saved_AddsOneCollection()
    {
        _collections.OpenApiImportResult = Collection.Create("petstore",
        [
            new HttpRequest { Method = HttpMethod.Get, RequestUri = "https://petstore.example/pets" }
        ]);
        var dialogs = RenderComponent<MudDialogProvider>();
        var cut = RenderLoadedPanel();

        var importButton = cut.FindComponents<SirenIconButton>().Single(b => b.Instance.Tooltip == "OpenApi Url Import");
        var prompt = importButton.Find("button").ClickAsync(new());
        dialogs.WaitForAssertion(() => dialogs.HasOpenDialog().ShouldBeTrue());
        dialogs.Find("input").Change("https://petstore.example/openapi.json");
        await dialogs.ClickButtonAsync("Import");
        dialogs.WaitForAssertion(() => dialogs.FindButton("Save").ShouldNotBeNull());
        await dialogs.ClickButtonAsync("Save");
        await prompt;

        _collections.GetCollections().Count.ShouldBe(1);
        cut.WaitForAssertion(() => cut.FindComponents<CollectionCard>().Count.ShouldBe(1));
        Services.GetRequiredService<ISnackbar>().ShownSnackbars.Count().ShouldBe(1);
    }

    [Fact(DisplayName = "A closed panel stops listening for collection changes")]
    public void Dispose_Unsubscribes()
    {
        RenderLoadedPanel();
        _collections.SubscriberCount.ShouldBe(1);

        DisposeComponents();

        _collections.SubscriberCount.ShouldBe(0);
    }
}
