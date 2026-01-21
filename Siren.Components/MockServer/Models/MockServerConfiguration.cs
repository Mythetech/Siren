namespace Siren.Components.MockServer.Models;

public class MockServerConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New Mock Server";

    public int Port { get; set; } = 9090;

    public bool EnableRequestLogging { get; set; } = true;

    public List<MockEndpoint> Endpoints { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    public int EndpointCount => Endpoints.Count;
}
