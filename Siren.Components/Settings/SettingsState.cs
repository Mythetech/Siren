using Microsoft.AspNetCore.Components;
using Mythetech.Framework.Infrastructure.Settings;
using Siren.Components.History;
using Siren.Components.Http;

namespace Siren.Components.Settings;

public enum TimeDisplayOptions
{
    Milliseconds,
    Seconds
}

public enum SizeDisplayOptions
{
    Bytes,
    Kilobytes,
    Megabytes
}

/// <summary>
/// Adapter class that provides backward compatibility with existing code
/// while delegating to the new framework settings infrastructure.
/// </summary>
public class SettingsState
{
    private readonly ISettingsProvider _provider;

    // Cached references for performance
    private HttpSettings? _httpSettings;
    private ResponseSettings? _responseSettings;
    private HistorySettings? _historySettings;
    private EnvironmentSettings? _environmentSettings;

    public EventCallback<SettingsState> SettingsCallback;
    public event Action<SettingsState>? SettingsChanged;

    public SettingsState(ISettingsProvider provider)
    {
        _provider = provider;
    }

    private HttpSettings Http => _httpSettings ??= _provider.GetSettings<HttpSettings>()!;
    private ResponseSettings Response => _responseSettings ??= _provider.GetSettings<ResponseSettings>()!;
    private HistorySettings History => _historySettings ??= _provider.GetSettings<HistorySettings>()!;
    private EnvironmentSettings Environment => _environmentSettings ??= _provider.GetSettings<EnvironmentSettings>()!;

    // HTTP Settings
    public int RequestTimeout
    {
        get => Http.RequestTimeout;
        set => Http.RequestTimeout = value;
    }

    public bool SendRequestsWithSystemToken
    {
        get => Http.SendRequestsWithSystemToken;
        set => Http.SendRequestsWithSystemToken = value;
    }

    public string DefaultUserAgent
    {
        get => Http.DefaultUserAgent;
        set => Http.DefaultUserAgent = value;
    }

    public string? DefaultHttpMethod
    {
        get => Http.DefaultHttpMethod;
        set => Http.DefaultHttpMethod = value;
    }

    public int RetryAttempts
    {
        get => Http.RetryAttempts;
        set => Http.RetryAttempts = value;
    }

    public int RetryDelayMs
    {
        get => Http.RetryDelayMs;
        set => Http.RetryDelayMs = value;
    }

    // Response Settings
    public TimeDisplayOptions TimeDisplay
    {
        get => Response.TimeDisplay;
        set => Response.TimeDisplay = value;
    }

    public SizeDisplayOptions SizeDisplay
    {
        get => Response.SizeDisplay;
        set => Response.SizeDisplay = value;
    }

    // History Settings
    public bool SaveHttpContent
    {
        get => History.SaveHttpContent;
        set => History.SaveHttpContent = value;
    }

    // Environment Settings
    /// <summary>
    /// Default environment to restore on startup. Also used as LastActiveEnvironment for API compat.
    /// </summary>
    public string? DefaultEnvironment
    {
        get => Environment.DefaultEnvironment;
        set
        {
            Environment.DefaultEnvironment = value;
            // Immediately persist environment settings changes
            Environment.MarkDirty();
            _ = _provider.NotifySettingsChangedAsync(Environment);
        }
    }

    /// <summary>
    /// Alias for DefaultEnvironment - maintains API compatibility with MainLayout.
    /// </summary>
    public string? LastActiveEnvironment
    {
        get => DefaultEnvironment;
        set => DefaultEnvironment = value;
    }

    /// <summary>
    /// Notifies listeners that settings have changed.
    /// Called after the settings dialog commits changes.
    /// </summary>
    public async Task NotifyChangedAsync()
    {
        await SettingsCallback.InvokeAsync(this);
        SettingsChanged?.Invoke(this);
    }
}

