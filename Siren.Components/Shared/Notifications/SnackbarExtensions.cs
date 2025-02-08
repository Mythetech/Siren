using MudBlazor;

namespace Siren.Components.Shared.Notifications;

public static class SnackbarExtensions
{
    public static void AddSirenNotification(this ISnackbar snackbar, string message, Severity severity = Severity.Info)
    {
        var parameters = new Dictionary<string, object>
        {
            { "Message", message },
            { "Severity", severity }
        };

        snackbar.Add<SirenNotificationToast>(parameters, severity);
    }

    public static void AddSirenInfo(this ISnackbar snackbar, string message)
        => snackbar.AddSirenNotification(message);
    
    public static void AddSirenWarning(this ISnackbar snackbar, string message)
        => snackbar.AddSirenNotification(message, Severity.Warning);
    
    public static void AddSirenError(this ISnackbar snackbar, string message)
        => snackbar.AddSirenNotification(message, Severity.Error);
    
    public static void AddSirenSuccess(this ISnackbar snackbar, string message)
        => snackbar.AddSirenNotification(message, Severity.Success);
}