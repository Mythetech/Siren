namespace Siren.Components.RequestContextPanel.Authentication;

public class RequestAuthenticationState
{
    public event Action? OnAuthenticationStateChanged;
    
    public void NotifyAuthenticationStateChanged() => OnAuthenticationStateChanged?.Invoke();

    public RequestAuthenticationState()
    {
        AuthType = AuthenticationType.None;
    }

    public AuthenticationType AuthType { get; set; }
    public Dictionary<string, string> AuthParams { get; set; } = new();
}

public enum AuthenticationType
{
    None,
    Bearer,
    Basic,
    ApiKey,
    OAuth2
}