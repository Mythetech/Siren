using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using AddNotification = Mythetech.Framework.Infrastructure.Commands.AddNotification;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using NSubstitute;
using Shouldly;
using Siren.Components.Collections;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.Http.Commands;
using Siren.Components.Http.Models;
using Siren.Components.NativeMenu;
using Siren.Components.NativeMenu.Commands;
using Siren.Components.NativeMenu.Consumers;
using Siren.Components.Services;
using Siren.Components.Shared.Dialogs.Commands;
using Siren.Components.Variables;
using Siren.Test.Infrastructure;

namespace Siren.Test.Components;

public class MenuCommandProviderTests : TestContext
{
    private const string TypedBody = """{"typed":"just now"}""";

    private readonly RequestTabsState _tabs;
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly IFileOpenService _fileOpen = Substitute.For<IFileOpenService>();
    private readonly IPluginInstaller _installer = Substitute.For<IPluginInstaller>();

    public MenuCommandProviderTests()
    {
        var variables = Substitute.For<IVariableService>();
        variables.GetVariables().Returns([]);
        variables.GetEnvironments().Returns([]);

        _tabs = this.AddRequestTabs(TestState.Settings(http => http.DefaultHttpMethod = "POST"));
        // Stands in for the request panel, whose editor hands over the last typed text when it is asked to commit
        _bus.PublishAsync(Arg.Any<CommitRequestEdits>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => _tabs.SetRawBody(_tabs.Active.Id, TypedBody));

        var pluginState = new PluginState();
        Services.AddSingleton(_bus);
        Services.AddSingleton(variables);
        Services.AddSingleton<VariableState>();
        Services.AddSingleton(Substitute.For<ICollectionsService>());
        Services.AddSingleton(Substitute.For<IHistoryService>());
        Services.AddSingleton(Substitute.For<ICurlGenerator>());
        Services.AddSingleton(_fileOpen);
        Services.AddSingleton(Substitute.For<IJsApiService>());
        Services.AddSingleton(pluginState);
        Services.AddSingleton(_installer);
        Services.AddSingleton<PluginMenuItemInvoker>();
        Services.AddSingleton<PluginMenuCallbackRegistry>();
        Services.AddSingleton(_ => new PluginContext
        {
            MessageBus = _bus,
            StateStore = new PluginStateStore(pluginState),
        });
        Services.AddLogging();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddMudServicesWithPopover();
    }

    [Fact(DisplayName = "Save Request saves text that was still in the editor when the menu was used")]
    public async Task SaveRequest_SavesTextStillInEditor()
    {
        SaveRequest? saved = null;
        _bus.PublishAsync(Arg.Do<SaveRequest>(command => saved = command)).Returns(Task.CompletedTask);
        _tabs.SetUri(_tabs.Active.Id, "https://api.example.com/users");
        var cut = RenderComponent<MenuCommandProvider>();

        await cut.Instance.Consume(new SaveActiveRequest());

        saved.ShouldNotBeNull().Body.ShouldBe(TypedBody);
    }

    [Fact(DisplayName = "Export as cURL exports text that was still in the editor when the menu was used")]
    public async Task ExportAsCurl_ExportsTextStillInEditor()
    {
        string? exportedBody = null;
        _bus.PublishAsync(Arg.Do<ShowDialog>(dialog =>
                exportedBody = ((HttpRequest)dialog.Parameters!["Request"]!).RawBody))
            .Returns(Task.CompletedTask);
        var cut = RenderComponent<MenuCommandProvider>();

        await cut.Instance.Consume(new ExportActiveRequestAsCurl());

        exportedBody.ShouldBe(TypedBody);
    }

    [Fact(DisplayName = "New Request from the menu opens a tab with the default method")]
    public async Task NewRequest_OpensTabWithDefaultMethod()
    {
        var cut = RenderComponent<MenuCommandProvider>();
        var before = _tabs.Tabs.Count;

        await cut.Instance.Consume(new NewRequest());

        _tabs.Tabs.Count.ShouldBe(before + 1);
        _tabs.Active.Request.ShouldNotBeNull().Method.ShouldBe(HttpMethod.Post);
    }

    [Fact(DisplayName = "Import Plugin imports each selected file")]
    public async Task ImportPlugin_EachSelectedFileIsImported()
    {
        string[] paths = ["/plugins/a.dll", "/plugins/b.dll"];
        _fileOpen.OpenFileAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<FileFilter[]?>()).Returns(paths);
        _installer.ImportFromFileAsync(Arg.Any<string>())
            .Returns(new PluginInstallResult(true, [], string.Empty));
        var cut = RenderComponent<MenuCommandProvider>();

        await cut.InvokeAsync(() => cut.Instance.Consume(new ImportPlugin()));

        foreach (var path in paths)
            await _installer.Received(1).ImportFromFileAsync(path);
    }

    [Fact(DisplayName = "Import Plugin imports the other files and notifies when one fails")]
    public async Task ImportPlugin_OneFails_OthersStillImportAndFailureIsNotified()
    {
        _fileOpen.OpenFileAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<FileFilter[]?>())
            .Returns(["/plugins/bad.dll", "/plugins/good.dll"]);
        _installer.ImportFromFileAsync("/plugins/bad.dll")
            .Returns(new PluginInstallResult(false, [], "Couldn't load a plugin from that file."));
        _installer.ImportFromFileAsync("/plugins/good.dll")
            .Returns(new PluginInstallResult(true, [], string.Empty));
        var cut = RenderComponent<MenuCommandProvider>();

        await cut.InvokeAsync(() => cut.Instance.Consume(new ImportPlugin()));

        await _installer.Received(1).ImportFromFileAsync("/plugins/bad.dll");
        await _installer.Received(1).ImportFromFileAsync("/plugins/good.dll");
        await _bus.Received(1).PublishAsync(Arg.Is<AddNotification>(n =>
            n.Message == "Couldn't load a plugin from that file." && n.Severity == Severity.Error));
    }
}
