using Siren.Components.MockServer.Models;

namespace Siren.Components.MockServer;

public interface IMockServerService
{
    // Server lifecycle
    Task<MockServerStartResult> StartAsync(Guid? configurationId = null);
    Task StopAsync();
    Task RestartAsync();

    // Server state
    MockServerStatus Status { get; }
    int? ActualPort { get; }
    string? BaseUrl { get; }
    Guid? ActiveConfigurationId { get; }

    // Configuration management
    MockServerConfiguration? GetActiveConfiguration();
    List<MockServerConfiguration> GetConfigurations();
    MockServerConfiguration? GetConfiguration(Guid id);
    void SaveConfiguration(MockServerConfiguration configuration);
    void DeleteConfiguration(Guid id);
    void SetActiveConfiguration(Guid id);
    MockServerConfiguration CreateConfiguration(string name);

    // Endpoint management
    void AddEndpoint(Guid configurationId, MockEndpoint endpoint);
    void UpdateEndpoint(Guid configurationId, MockEndpoint endpoint);
    void RemoveEndpoint(Guid configurationId, Guid endpointId);
    MockEndpoint? GetEndpoint(Guid configurationId, Guid endpointId);

    // Request logging
    IReadOnlyList<MockRequestLog> GetRequestLogs();
    void ClearRequestLogs();

    // Events
    event Action<MockServerStatus>? StatusChanged;
    event Action<MockRequestLog>? RequestReceived;
    event Action<MockServerConfiguration>? ConfigurationChanged;
    event Action? ConfigurationsChanged;
}
