namespace Siren.Components.MockServer.Models;

public class MockEndpoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Method { get; set; } = "GET";

    public string RoutePattern { get; set; } = "/";

    public MockResponse Response { get; set; } = new();

    public bool IsEnabled { get; set; } = true;

    public int Priority { get; set; }

    public Guid? SourceRequestId { get; set; }

    public string DisplayText => $"{Method} {RoutePattern}";
}
