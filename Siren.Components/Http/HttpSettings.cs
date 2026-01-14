using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.Theme;

namespace Siren.Components.Http;

/// <summary>
/// Settings for HTTP request behavior.
/// </summary>
public class HttpSettings : SettingsBase
{
    public override string SettingsId => "Http";
    public override string DisplayName => "Requests";
    public override string Icon => SirenIcons.Request;
    public override int Order => 10;

    [Setting(Label = "Send with Siren Token header",
        Group = "General",
        Order = 1,
        Description = "Recommended to easily flag requests as test requests")]
    public bool SendRequestsWithSystemToken { get; set; } = true;

    [Setting(Label = "Default User-Agent",
        Group = "General",
        Order = 2,
        Description = "Sent with requests unless a custom User-Agent header is specified")]
    public string DefaultUserAgent { get; set; } = "siren/0.1";

    [Setting(Label = "Default HTTP Method",
        Group = "General",
        Order = 3,
        Options = "GET,POST,PUT,PATCH,DELETE,HEAD,OPTIONS",
        Description = "HTTP method to use when creating new requests")]
    public string? DefaultHttpMethod { get; set; }

    [Setting(Label = "Request Timeout (seconds)",
        Group = "General",
        Order = 4,
        Min = 1,
        Max = 120,
        Step = 1,
        Description = "Maximum time to wait for a response")]
    public int RequestTimeout { get; set; } = 10;

    [Setting(Label = "Retry Attempts",
        Group = "Retry",
        Order = 1,
        Min = 0,
        Max = 10,
        Step = 1,
        Description = "Number of times to retry failed requests (0 = no retries)")]
    public int RetryAttempts { get; set; } = 0;

    [Setting(Label = "Retry Delay (ms)",
        Group = "Retry",
        Order = 2,
        Min = 100,
        Max = 30000,
        Step = 100,
        Description = "Delay in milliseconds between retry attempts")]
    public int RetryDelayMs { get; set; } = 1000;
}
