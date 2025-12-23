namespace Siren.Components.Http.Commands
{
    public record SaveRequest(
        string Method,
        string RequestUri,
        string DisplayUri,
        Dictionary<string, string>? QueryParameters,
        Dictionary<string, string> Headers,
        Dictionary<string, string> FormData,
        string ContentType,
        string? Body,
        string BodyType,
        TimeSpan Timeout,
        int RetryAttempts,
        Guid Id
    );
}

