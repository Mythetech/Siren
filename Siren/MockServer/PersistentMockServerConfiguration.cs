using Siren.Components.MockServer.Models;

namespace Siren.MockServer;

public class PersistentMockServerConfiguration : MockServerConfiguration
{
    public int DocumentId { get; set; }

    public static PersistentMockServerConfiguration Create(MockServerConfiguration configuration)
    {
        return new PersistentMockServerConfiguration
        {
            Id = configuration.Id,
            Name = configuration.Name,
            Port = configuration.Port,
            EnableRequestLogging = configuration.EnableRequestLogging,
            Endpoints = configuration.Endpoints,
            CreatedAt = configuration.CreatedAt,
            ModifiedAt = configuration.ModifiedAt
        };
    }

    public MockServerConfiguration ToConfiguration()
    {
        return this;
    }
}
