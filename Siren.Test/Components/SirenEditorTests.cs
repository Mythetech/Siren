using System.Text.Json;
using BlazorMonaco.Editor;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Siren.Components;
using Siren.Components.Shared;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class SirenEditorTests : TestContext
{
    private const string Create = "blazorMonaco.editor.create";
    private const string GetValue = "blazorMonaco.editor.getValue";
    private const string SetValue = "blazorMonaco.editor.setValue";
    private const string SetEventListener = "blazorMonaco.editor.setEventListener";

    private readonly SirenAppState _appState = TestState.AppState();

    public SirenEditorTests()
    {
        Services.AddLogging();
        Services.AddSingleton(_appState);
        this.AddReadyJsGuards();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    // Monaco tells BlazorMonaco about a change by calling this method from JavaScript
    private static Task RaiseEditorChangeAsync(IRenderedComponent<SirenEditor> cut)
    {
        var monaco = cut.FindComponent<StandaloneCodeEditor>();
        return cut.InvokeAsync(() => monaco.Instance.EventCallback("OnDidChangeModelContent", "{}"));
    }

    [Fact(DisplayName = "The editor is created with its text")]
    public void Editor_IsCreatedWithItsText()
    {
        RenderComponent<SirenEditor>(p => p.Add(x => x.Text, """{"name":"Ada"}"""));

        var options = (JsonElement)JSInterop.Invocations[Create].ShouldHaveSingleItem().Arguments[1]!;
        options.GetProperty("value").GetString().ShouldBe("""{"name":"Ada"}""");
        JSInterop.Invocations[SetValue].ShouldBeEmpty();
    }

    [Theory(DisplayName = "A new editor takes the theme of the app's color mode")]
    [InlineData(true, "vs-dark")]
    [InlineData(false, "vs")]
    public void NewEditor_TakesTheAppsColorMode(bool isDarkMode, string theme)
    {
        _appState.IsDarkMode = isDarkMode;

        RenderComponent<SirenEditor>();

        var options = (JsonElement)JSInterop.Invocations[Create].ShouldHaveSingleItem().Arguments[1]!;
        options.GetProperty("theme").GetString().ShouldBe(theme);
    }

    [Fact(DisplayName = "A change in the editor is reported with the document it belongs to")]
    public async Task EditorChange_IsReportedWithItsDocument()
    {
        var documentId = Guid.NewGuid();
        var reported = new List<EditorTextChange>();
        JSInterop.Setup<string>(GetValue, _ => true).SetResult("typed");
        var cut = RenderComponent<SirenEditor>(p => p
            .Add(x => x.DocumentId, documentId)
            .Add(x => x.ReadOnly, false)
            .Add(x => x.TextChanged, (EditorTextChange change) => reported.Add(change)));

        await RaiseEditorChangeAsync(cut);

        reported.ShouldBe([new EditorTextChange(documentId, "typed")]);
    }

    [Fact(DisplayName = "Text the editor reported is not written back to it")]
    public async Task ReportedText_IsNotWrittenBack()
    {
        JSInterop.Setup<string>(GetValue, _ => true).SetResult("typed");
        var cut = RenderComponent<SirenEditor>(p => p
            .Add(x => x.ReadOnly, false)
            .Add(x => x.TextChanged, (EditorTextChange _) => { }));
        await RaiseEditorChangeAsync(cut);

        cut.SetParametersAndRender(p => p.Add(x => x.Text, "typed"));

        JSInterop.Invocations[SetValue].ShouldBeEmpty();
    }

    [Fact(DisplayName = "New text is written to the editor")]
    public void NewText_IsWritten()
    {
        var cut = RenderComponent<SirenEditor>(p => p.Add(x => x.Text, "first"));

        cut.SetParametersAndRender(p => p.Add(x => x.Text, "second"));

        JSInterop.Invocations[SetValue].ShouldHaveSingleItem().Arguments[1].ShouldBe("second");
    }

    [Fact(DisplayName = "The same text is not written again")]
    public void SameText_IsNotWritten()
    {
        var cut = RenderComponent<SirenEditor>(p => p.Add(x => x.Text, "first"));

        cut.SetParametersAndRender(p => p.Add(x => x.Text, "first"));

        JSInterop.Invocations[SetValue].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Another document is written even when its text is the same")]
    public void OtherDocument_IsWritten()
    {
        var cut = RenderComponent<SirenEditor>(p => p
            .Add(x => x.DocumentId, Guid.NewGuid())
            .Add(x => x.Text, "same"));

        cut.SetParametersAndRender(p => p.Add(x => x.DocumentId, Guid.NewGuid()));

        JSInterop.Invocations[SetValue].ShouldHaveSingleItem().Arguments[1].ShouldBe("same");
    }

    [Fact(DisplayName = "An editor without a TextChanged handler does not listen for changes")]
    public void NoHandler_DoesNotListen()
    {
        RenderComponent<SirenEditor>();

        JSInterop.Invocations[SetEventListener].ShouldBeEmpty();
    }

    [Fact(DisplayName = "An editor with a TextChanged handler listens for changes")]
    public void Handler_Listens()
    {
        RenderComponent<SirenEditor>(p => p.Add(x => x.TextChanged, (EditorTextChange _) => { }));

        JSInterop.Invocations[SetEventListener].ShouldHaveSingleItem().Arguments[1].ShouldBe("OnDidChangeModelContent");
    }

    [Fact(DisplayName = "Flush waits for a read that is still out")]
    public async Task Flush_WaitsForOutstandingRead()
    {
        var read = JSInterop.Setup<string>(GetValue, _ => true);
        var reported = new List<EditorTextChange>();
        var cut = RenderComponent<SirenEditor>(p => p
            .Add(x => x.ReadOnly, false)
            .Add(x => x.TextChanged, (EditorTextChange change) => reported.Add(change)));
        var change = RaiseEditorChangeAsync(cut);

        var flush = cut.InvokeAsync(() => cut.Instance.FlushAsync());
        flush.IsCompleted.ShouldBeFalse();
        read.SetResult("typed");
        await Task.WhenAll(change, flush);

        reported.Select(c => c.Text).ShouldBe(["typed"]);
    }
}
