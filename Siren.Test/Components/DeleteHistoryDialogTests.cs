using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Shouldly;
using Siren.Components.History;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class DeleteHistoryDialogTests : TestContext
{
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();

    public DeleteHistoryDialogTests()
    {
        Services.AddSingleton(_historyService);
        Services.AddMudServices();
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private ISnackbar Snackbar => Services.GetRequiredService<ISnackbar>();

    [Fact(DisplayName = "Confirming deletes the history")]
    public async Task Confirm_DeletesHistory()
    {
        var provider = await this.OpenDialogAsync<DeleteHistoryDialog>();

        await provider.ClickButtonAsync("Delete");

        _historyService.Received(1).DeleteAllHistoryRecords();
    }

    [Fact(DisplayName = "Confirming closes the dialog")]
    public async Task Confirm_ClosesDialog()
    {
        var provider = await this.OpenDialogAsync<DeleteHistoryDialog>();

        await provider.ClickButtonAsync("Delete");

        provider.HasOpenDialog().ShouldBeFalse();
    }

    [Fact(DisplayName = "Confirming reports that the history was deleted")]
    public async Task Confirm_ReportsSuccess()
    {
        var provider = await this.OpenDialogAsync<DeleteHistoryDialog>();

        await provider.ClickButtonAsync("Delete");

        Snackbar.ShownSnackbars.ShouldHaveSingleItem().Severity.ShouldBe(Severity.Success);
    }

    [Fact(DisplayName = "Cancelling leaves the history alone")]
    public async Task Cancel_KeepsHistory()
    {
        var provider = await this.OpenDialogAsync<DeleteHistoryDialog>();

        await provider.ClickButtonAsync("Cancel");

        _historyService.DidNotReceive().DeleteAllHistoryRecords();
        Snackbar.ShownSnackbars.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A failed delete is reported as an error")]
    public async Task Confirm_WhenDeleteFails_ReportsError()
    {
        _historyService.When(s => s.DeleteAllHistoryRecords()).Do(_ => throw new InvalidOperationException("database is locked"));
        var provider = await this.OpenDialogAsync<DeleteHistoryDialog>();

        await provider.ClickButtonAsync("Delete");

        Snackbar.ShownSnackbars.ShouldHaveSingleItem().Severity.ShouldBe(Severity.Error);
        provider.HasOpenDialog().ShouldBeFalse();
    }
}
