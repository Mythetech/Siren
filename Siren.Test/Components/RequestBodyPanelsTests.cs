using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Files;
using NSubstitute;
using Shouldly;
using Siren.Components.Http;
using Siren.Components.Http.Models;
using Siren.Components.RequestContextPanel;
using Siren.Components.Services;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class RequestBodyPanelsTests : TestContext
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"siren-attachment-{Guid.NewGuid():N}.bin");
    private readonly RequestTabsState _tabs;
    private int _requestChanges;

    public RequestBodyPanelsTests()
    {
        File.WriteAllBytes(_file, [1, 2, 3]);
        var files = Substitute.For<IFileOpenService>();
        files.OpenFileAsync().ReturnsForAnyArgs([_file]);

        _tabs = this.AddRequestTabs();
        _tabs.RequestChanged += _ => _requestChanges++;
        Services.AddSingleton(files);
        Services.AddSingleton(Substitute.For<IJsApiService>());
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    protected override void Dispose(bool disposing)
    {
        File.Delete(_file);
        base.Dispose(disposing);
    }

    private static Task ClickAsync<TComponent>(IRenderedComponent<TComponent> cut, string tooltip)
        where TComponent : IComponent
    {
        var button = cut.FindComponents<SirenIconButton>().Single(b => b.Instance.Tooltip == tooltip);
        return button.Find("button").ClickAsync(new MouseEventArgs());
    }

    private IRenderedFragment RenderFormPanel(RequestBodyType bodyType) => bodyType == RequestBodyType.MultipartFormData
        ? RenderComponent<MultipartFormDataPanel>(p => p.Add(x => x.NetworkRequest, _tabs.Active))
        : RenderComponent<FormDataPanel>(p => p.Add(x => x.NetworkRequest, _tabs.Active));

    private static Task AddFieldAsync(IRenderedFragment cut, RequestBodyType bodyType)
    {
        var tooltip = bodyType == RequestBodyType.MultipartFormData ? "Add Form Field" : "Add Form Data Entry";
        var button = cut.FindComponents<SirenIconButton>().Single(b => b.Instance.Tooltip == tooltip);
        return button.Find("button").ClickAsync(new MouseEventArgs());
    }

    private static IReadOnlyList<DictionaryViewModel> FormRows(IRenderedFragment cut)
        => (cut.FindComponent<MudDataGrid<DictionaryViewModel>>().Instance.Items ?? []).ToList();

    private static IEnumerable<bool> Marks(IRenderedFragment cut)
        => cut.FindComponents<NotSentMark>().Select(mark => mark.Instance.Shown);

    /// <summary>
    /// The text boxes of the form rows in reading order: each row's name, then its value.
    /// </summary>
    private static IReadOnlyList<IElement> CellInputs(IRenderedFragment cut)
        => cut.FindComponent<MudDataGrid<DictionaryViewModel>>().FindAll("input")
            .Where(input => input.GetAttribute("type") == "text").ToList();

    [Fact(DisplayName = "Adding a form field stores it on the tab through the state")]
    public async Task FormData_AddField_StoresOnTab()
    {
        var cut = RenderComponent<FormDataPanel>(p => p.Add(x => x.NetworkRequest, _tabs.Active));

        await ClickAsync(cut, "Add Form Data Entry");

        _tabs.Active.Request.ShouldNotBeNull().FormData.ShouldBe(new Dictionary<string, string> { ["Key"] = "Value" });
        _requestChanges.ShouldBe(1);
    }

    [Fact(DisplayName = "Adding a multipart field stores it on the tab through the state")]
    public async Task Multipart_AddField_StoresOnTab()
    {
        var cut = RenderComponent<MultipartFormDataPanel>(p => p.Add(x => x.NetworkRequest, _tabs.Active));

        await ClickAsync(cut, "Add Form Field");

        _tabs.Active.Request.ShouldNotBeNull().FormData.ShouldBe(new Dictionary<string, string> { ["Key"] = "Value" });
        _requestChanges.ShouldBe(1);
    }

    [Fact(DisplayName = "Adding a multipart file stores it on the tab through the state")]
    public async Task Multipart_AddFile_StoresOnTab()
    {
        var cut = RenderComponent<MultipartFormDataPanel>(p => p.Add(x => x.NetworkRequest, _tabs.Active));

        await ClickAsync(cut, "Add File Attachment");

        var attachment = _tabs.Active.Request.ShouldNotBeNull().BinaryAttachments.ShouldHaveSingleItem();
        attachment.FileName.ShouldBe(Path.GetFileName(_file));
        attachment.Data.ShouldBe([1, 2, 3]);
        _requestChanges.ShouldBe(1);
    }

    [Fact(DisplayName = "Adding a binary attachment stores it on the tab through the state")]
    public async Task Binary_AddFile_StoresOnTab()
    {
        var cut = RenderComponent<BinaryAttachmentPanel>(p => p.Add(x => x.NetworkRequest, _tabs.Active));

        await ClickAsync(cut, "Add File Attachment");

        var attachment = _tabs.Active.Request.ShouldNotBeNull().BinaryAttachments.ShouldHaveSingleItem();
        attachment.FileName.ShouldBe(Path.GetFileName(_file));
        _requestChanges.ShouldBe(1);
    }

    [Theory(DisplayName = "Adding two fields keeps both rows and sends the last")]
    [InlineData(RequestBodyType.FormData)]
    [InlineData(RequestBodyType.MultipartFormData)]
    public async Task TwoAdds_KeepBothRows(RequestBodyType bodyType)
    {
        var cut = RenderFormPanel(bodyType);

        await AddFieldAsync(cut, bodyType);
        await AddFieldAsync(cut, bodyType);

        FormRows(cut).Count.ShouldBe(2);
        _tabs.Active.Request.ShouldNotBeNull().FormData.ShouldBe(new Dictionary<string, string> { ["Key"] = "Value" });
    }

    [Theory(DisplayName = "The earlier of two rows with one name is marked as not sent")]
    [InlineData(RequestBodyType.FormData)]
    [InlineData(RequestBodyType.MultipartFormData)]
    public async Task EarlierRow_IsMarked(RequestBodyType bodyType)
    {
        var cut = RenderFormPanel(bodyType);

        await AddFieldAsync(cut, bodyType);
        await AddFieldAsync(cut, bodyType);

        Marks(cut).ShouldBe([true, false]);
    }

    [Theory(DisplayName = "Renaming the later row clears the mark")]
    [InlineData(RequestBodyType.FormData)]
    [InlineData(RequestBodyType.MultipartFormData)]
    public async Task RenamingLaterRow_ClearsMark(RequestBodyType bodyType)
    {
        var cut = RenderFormPanel(bodyType);
        await AddFieldAsync(cut, bodyType);
        await AddFieldAsync(cut, bodyType);

        await CellInputs(cut)[2].InputAsync(new ChangeEventArgs { Value = "Other" });

        Marks(cut).ShouldBe([false, false]);
        _tabs.Active.Request.ShouldNotBeNull().FormData
            .ShouldBe(new Dictionary<string, string> { ["Key"] = "Value", ["Other"] = "Value" });
    }

    [Theory(DisplayName = "Typing in a field reaches the tab before the cell is left")]
    [InlineData(RequestBodyType.FormData)]
    [InlineData(RequestBodyType.MultipartFormData)]
    public async Task Typing_ReachesTabBeforeCellIsLeft(RequestBodyType bodyType)
    {
        _tabs.SetFormData(_tabs.Active.Id, new Dictionary<string, string> { ["name"] = "Ada" });
        var cut = RenderFormPanel(bodyType);
        var rows = FormRows(cut);

        await CellInputs(cut)[1].InputAsync(new ChangeEventArgs { Value = "Grace" });

        _tabs.Active.Request.ShouldNotBeNull().FormData.ShouldBe(new Dictionary<string, string> { ["name"] = "Grace" });
        // The grid keys a row by its object, so a new object would throw away the cell being typed in
        FormRows(cut).ShouldBe(rows);
    }

    [Theory(DisplayName = "A field whose name is emptied while typing stays and is not sent")]
    [InlineData(RequestBodyType.FormData)]
    [InlineData(RequestBodyType.MultipartFormData)]
    public async Task EmptiedName_StaysAndIsNotSent(RequestBodyType bodyType)
    {
        _tabs.SetFormData(_tabs.Active.Id, new Dictionary<string, string> { ["name"] = "Ada", ["role"] = "admin" });
        var cut = RenderFormPanel(bodyType);

        await CellInputs(cut)[0].InputAsync(new ChangeEventArgs { Value = "" });

        FormRows(cut).Count.ShouldBe(2);
        _tabs.Active.Request.ShouldNotBeNull().FormData.ShouldBe(new Dictionary<string, string> { ["role"] = "admin" });
    }

    [Fact(DisplayName = "The not sent mark tells a screen reader what it means")]
    public void NotSentMark_HasTextForScreenReaders()
    {
        var cut = RenderComponent<NotSentMark>(p => p.Add(x => x.Shown, true));

        cut.Find(".sr-only").TextContent.ShouldBe("Not sent: a later row has the same name");
    }
}
