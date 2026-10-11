using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Siren.Test.Infrastructure;

public static class DialogTestExtensions
{
    /// <summary>
    /// Opens <typeparamref name="TDialog"/> through the dialog service, the same path the app uses,
    /// and returns the provider that hosts it.
    /// </summary>
    public static async Task<IRenderedComponent<MudDialogProvider>> OpenDialogAsync<TDialog>(this TestContext context, string title = "Dialog")
        where TDialog : IComponent
    {
        var provider = context.RenderComponent<MudDialogProvider>();
        var dialogs = context.Services.GetRequiredService<IDialogService>();

        await provider.InvokeAsync(() => dialogs.ShowAsync<TDialog>(title));

        return provider;
    }

    public static IElement FindButton(this IRenderedFragment fragment, string text)
        => fragment.FindAll("button").First(b => b.TextContent.Trim() == text);

    public static Task ClickButtonAsync(this IRenderedFragment fragment, string text)
        => fragment.FindButton(text).ClickAsync(new MouseEventArgs());

    public static bool HasOpenDialog(this IRenderedFragment fragment)
        => fragment.FindAll(".mud-dialog").Count > 0;
}
