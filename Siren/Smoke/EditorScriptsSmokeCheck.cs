using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Guards;
using Mythetech.Framework.Infrastructure.Smoke;

namespace Siren.Smoke;

/// <summary>
/// Passes when Monaco loaded in the WebView, through the "monaco" JS guard that index.html registers after
/// the editor scripts. A publish that drops a static asset still renders the shell, so without this the
/// smoke run would pass an app whose request and response editors cannot open.
/// </summary>
public sealed class EditorScriptsSmokeCheck(IJsGuardService guards, IJSRuntime js) : ISmokeCheck
{
    private static readonly TimeSpan GuardTimeout = TimeSpan.FromSeconds(15);

    public string Name => "siren/editor-scripts";

    // Longer than the guard's own wait, so a missing script reads as "did not load" rather than a timeout.
    public TimeSpan Timeout => TimeSpan.FromSeconds(20);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!await guards.WaitForReadyAsync(js, "monaco", GuardTimeout))
            throw new InvalidOperationException("Monaco did not load in the WebView");
    }
}
