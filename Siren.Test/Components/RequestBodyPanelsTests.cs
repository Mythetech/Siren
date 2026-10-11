using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Files;
using NSubstitute;
using Shouldly;
using Siren.Components.Http;
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
}
