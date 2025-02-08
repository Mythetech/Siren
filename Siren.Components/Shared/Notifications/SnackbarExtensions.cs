using MudBlazor;

namespace Siren.Components.Shared.Notifications;

public static class SnackbarExtensions
{
    public static void AddSirenNotification(this ISnackbar snackbar, string message, Severity severity = Severity.Info, Action<SnackbarOptions>? configuration = null)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Message", message },
            { "Severity", severity }
        };

        snackbar.Add<SirenNotificationToast>(parameters, severity, configuration);
    }

    public static void AddSirenInfo(this ISnackbar snackbar, string message, Action<SnackbarOptions>? configuration = null)
        => snackbar.AddSirenNotification(message, Severity.Info, configuration);
    
    public static void AddSirenWarning(this ISnackbar snackbar, string message, Action<SnackbarOptions>? configuration = null)
        => snackbar.AddSirenNotification(message, Severity.Warning, configuration);
    
    public static void AddSirenError(this ISnackbar snackbar, string message, Action<SnackbarOptions>? configuration = null)
        => snackbar.AddSirenNotification(message, Severity.Error, configuration);
    
    public static void AddSirenSuccess(this ISnackbar snackbar, string message, Action<SnackbarOptions>? configuration = null)
        => snackbar.AddSirenNotification(message, Severity.Success, configuration);

    public static void AddSuccessWithUndo(this ISnackbar snackbar, string message, Action undoCallback)
    {
        const int undoActionVisibleStateDuration = 5000;
        
        var parameters = new Dictionary<string, object>
        {
            { "Message", message },
            { "Severity", Severity.Success },
            {"VisibleDuration", undoActionVisibleStateDuration }
        };

        snackbar.Add<SirenNotificationToast>(parameters, Severity.Success, opts =>
        {
            opts.Action = "Undo";
            opts.ActionColor = Color.Success;
            opts.ActionVariant = Variant.Text;
            opts.VisibleStateDuration = undoActionVisibleStateDuration; 
            opts.OnClick = _ =>
            {
                undoCallback?.Invoke();
                return Task.CompletedTask;
            };
        });
    }
}