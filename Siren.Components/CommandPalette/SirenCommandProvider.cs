using Microsoft.AspNetCore.Components;
using Mythetech.Framework.Components.CommandPalette;
using Siren.Components.Http;
using Siren.Components.Theme;

namespace Siren.Components.CommandPalette;

public sealed class SirenCommandProvider : ICommandProvider
{
    private readonly SirenAppState _appState;
    private readonly NavigationManager _nav;
    private readonly RequestTabsState _tabs;
    private readonly IRequestSender _sender;

    public SirenCommandProvider(SirenAppState appState, NavigationManager nav, RequestTabsState tabs, IRequestSender sender)
    {
        _appState = appState;
        _nav = nav;
        _tabs = tabs;
        _sender = sender;
    }

    public ValueTask<IReadOnlyList<PaletteCommand>> GetCommandsAsync(
        string query, CancellationToken ct)
    {
        var commands = new List<PaletteCommand>
        {
            new(
                Id: "nav.home",
                Title: "Go to Requests",
                Description: "Ctrl+Shift+R",
                Icon: SirenIcons.Home,
                Keywords: ["home", "index", "requests", "main"],
                InvokeAsync: _ => { _nav.NavigateTo("/"); return Task.CompletedTask; },
                Group: "Navigation"),

            new(
                Id: "nav.mock-server",
                Title: "Go to Mock Server",
                Description: "Ctrl+Shift+M",
                Icon: SirenIcons.MockServer,
                Keywords: ["mock", "server", "stub", "fake"],
                InvokeAsync: _ => { _nav.NavigateTo("/mock-server"); return Task.CompletedTask; },
                Group: "Navigation"),

            new(
                Id: "panel.history",
                Title: "Open History",
                Description: "Ctrl+Shift+H",
                Icon: SirenIcons.History,
                Keywords: ["history", "past", "recent", "log"],
                InvokeAsync: _ => _appState.TriggerOpenContextPanel("History"),
                Group: "Panels"),

            new(
                Id: "panel.collections",
                Title: "Open Collections",
                Description: "Ctrl+Shift+C",
                Icon: SirenIcons.Collections,
                Keywords: ["collections", "saved", "folders", "organize"],
                InvokeAsync: _ => _appState.TriggerOpenContextPanel("Collections"),
                Group: "Panels"),

            new(
                Id: "panel.variables",
                Title: "Open Variables",
                Description: "Ctrl+Shift+V",
                Icon: SirenIcons.Variables,
                Keywords: ["variables", "environment", "env", "substitution"],
                InvokeAsync: _ => _appState.TriggerOpenContextPanel("Variables"),
                Group: "Panels"),

            new(
                Id: "panel.mock-server",
                Title: "Open Mock Server Panel",
                Description: null,
                Icon: SirenIcons.MockServer,
                Keywords: ["mock", "server", "panel", "sidebar"],
                InvokeAsync: _ => _appState.TriggerOpenContextPanel("Mock Server"),
                Group: "Panels"),

            new(
                Id: "context.auth",
                Title: "Open Auth Panel",
                Description: null,
                Icon: SirenIcons.Auth,
                Keywords: ["authorization", "auth", "bearer", "token", "oauth"],
                InvokeAsync: _ => NavigateAndOpenRequestContext("Auth"),
                Group: "Request Context"),

            new(
                Id: "context.headers",
                Title: "Open Headers Panel",
                Description: null,
                Icon: SirenIcons.Headers,
                Keywords: ["headers", "content-type", "accept"],
                InvokeAsync: _ => NavigateAndOpenRequestContext("Headers"),
                Group: "Request Context"),

            new(
                Id: "context.cookies",
                Title: "Open Cookies Panel",
                Description: null,
                Icon: SirenIcons.Cookies,
                Keywords: ["cookies", "session", "set-cookie"],
                InvokeAsync: _ => NavigateAndOpenRequestContext("Cookies"),
                Group: "Request Context"),

            new(
                Id: "action.send-request",
                Title: "Send Request",
                Description: "Ctrl+Enter",
                Icon: SirenIcons.Play,
                Keywords: ["send", "run", "execute", "http"],
                InvokeAsync: _ => SendActiveRequestAsync(),
                Group: "Actions"),

            new(
                Id: "action.new-tab",
                Title: "New Tab",
                Description: "Ctrl+T",
                Icon: SirenIcons.Add,
                Keywords: ["new", "tab", "request", "create"],
                InvokeAsync: _ => OpenNewTab(),
                Group: "Actions"),

            new(
                Id: "app.settings",
                Title: "Open Settings",
                Description: "Ctrl+S",
                Icon: SirenIcons.Settings,
                Keywords: ["settings", "preferences", "configuration", "options"],
                InvokeAsync: _ => _appState.TriggerOpenSettings(),
                Group: "Settings"),
        };

        return ValueTask.FromResult<IReadOnlyList<PaletteCommand>>(commands);
    }

    private async Task NavigateAndOpenRequestContext(string tab)
    {
        // The request context panel belongs to the requests page, which needs a moment to render
        if (NavigateHome())
            await Task.Delay(100);

        await _appState.TriggerOpenRequestContextPanel(tab);
    }

    private async Task SendActiveRequestAsync()
    {
        NavigateHome();
        await _sender.SendAsync(_tabs.Active.Id);
    }

    private Task OpenNewTab()
    {
        NavigateHome();
        _tabs.OpenNew();
        return Task.CompletedTask;
    }

    private bool NavigateHome()
    {
        var relativePath = _nav.ToBaseRelativePath(_nav.Uri);
        if (relativePath is "" or "/" or "index")
            return false;

        _nav.NavigateTo("/");
        return true;
    }
}
