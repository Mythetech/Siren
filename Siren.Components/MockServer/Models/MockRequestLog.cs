namespace Siren.Components.MockServer.Models;

public class MockRequestLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string QueryString { get; set; } = string.Empty;

    public Dictionary<string, string> Headers { get; set; } = new();

    public string? Body { get; set; }

    public Guid? MatchedEndpointId { get; set; }

    public int ResponseStatusCode { get; set; }

    public long ProcessingTimeMs { get; set; }

    public string DisplayText => $"{Method} {Path}";

    public bool WasMatched => MatchedEndpointId.HasValue;
}
