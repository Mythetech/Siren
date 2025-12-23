using MudBlazor;

namespace Siren.Components.Shared.Dialogs;

public static class SirenDialogs
{
    public static DialogOptions CreateDefaultOptions(MaxWidth maxWidth = MaxWidth.Small)
        => new DialogOptions
        {
            CloseOnEscapeKey = true,
            BackgroundClass = "siren-dialog",
            MaxWidth = maxWidth,
            FullWidth = true,
            CloseButton = true,
        };
}