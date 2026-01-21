using Siren.Components.MockServer.Models;

namespace Siren.Components.MockServer;

public class MockServerState : IDisposable
{
    private readonly IMockServerService _mockServerService;
    private List<MockServerConfiguration>? _configurations;
    private readonly List<MockRequestLog> _requestLogs = new();

    public MockServerState(IMockServerService mockServerService)
    {
        _mockServerService = mockServerService;
        _mockServerService.StatusChanged += OnStatusChanged;
        _mockServerService.RequestReceived += OnRequestReceived;
        _mockServerService.ConfigurationChanged += OnConfigurationChanged;
        _mockServerService.ConfigurationsChanged += OnConfigurationsChanged;
    }

    // Server status
    public MockServerStatus Status => _mockServerService.Status;
    public int? ActualPort => _mockServerService.ActualPort;
    public string? BaseUrl => _mockServerService.BaseUrl;
    public bool IsRunning => Status == MockServerStatus.Running;
    public bool IsStopped => Status == MockServerStatus.Stopped;
    public Guid? ActiveConfigurationId => _mockServerService.ActiveConfigurationId;

    // Configuration access
    public MockServerConfiguration? ActiveConfiguration => _mockServerService.GetActiveConfiguration();

    public List<MockServerConfiguration> Configurations
    {
        get
        {
            _configurations ??= _mockServerService.GetConfigurations();
            return _configurations;
        }
    }

    // Request logs
    public IReadOnlyList<MockRequestLog> RequestLogs => _requestLogs.AsReadOnly();

    // Events for UI updates
    public event Action? StateChanged;
    public event Action<MockRequestLog>? RequestLogged;

    // Server lifecycle actions
    public async Task StartServerAsync(Guid? configurationId = null)
    {
        await _mockServerService.StartAsync(configurationId);
    }

    public async Task StopServerAsync()
    {
        await _mockServerService.StopAsync();
    }

    public async Task RestartServerAsync()
    {
        await _mockServerService.RestartAsync();
    }

    // Configuration actions
    public MockServerConfiguration? GetConfiguration(Guid id)
    {
        return _mockServerService.GetConfiguration(id);
    }

    public MockServerConfiguration CreateConfiguration(string name)
    {
        var config = _mockServerService.CreateConfiguration(name);
        Refresh();
        return config;
    }

    public void SaveConfiguration(MockServerConfiguration config)
    {
        _mockServerService.SaveConfiguration(config);
        Refresh();
    }

    public void DeleteConfiguration(Guid id)
    {
        _mockServerService.DeleteConfiguration(id);
        Refresh();
    }

    public void SetActiveConfiguration(Guid id)
    {
        _mockServerService.SetActiveConfiguration(id);
        Refresh();
    }

    // Endpoint actions
    public void AddEndpoint(Guid configurationId, MockEndpoint endpoint)
    {
        _mockServerService.AddEndpoint(configurationId, endpoint);
        Refresh();
    }

    public void UpdateEndpoint(Guid configurationId, MockEndpoint endpoint)
    {
        _mockServerService.UpdateEndpoint(configurationId, endpoint);
        Refresh();
    }

    public void RemoveEndpoint(Guid configurationId, Guid endpointId)
    {
        _mockServerService.RemoveEndpoint(configurationId, endpointId);
        Refresh();
    }

    public MockEndpoint? GetEndpoint(Guid configurationId, Guid endpointId)
    {
        return _mockServerService.GetEndpoint(configurationId, endpointId);
    }

    // Log actions
    public void ClearLogs()
    {
        _requestLogs.Clear();
        _mockServerService.ClearRequestLogs();
        StateChanged?.Invoke();
    }

    // Event handlers
    private void OnStatusChanged(MockServerStatus status)
    {
        StateChanged?.Invoke();
    }

    private void OnRequestReceived(MockRequestLog log)
    {
        _requestLogs.Insert(0, log);
        if (_requestLogs.Count > 100)
        {
            _requestLogs.RemoveAt(_requestLogs.Count - 1);
        }

        RequestLogged?.Invoke(log);
        StateChanged?.Invoke();
    }

    private void OnConfigurationChanged(MockServerConfiguration config)
    {
        Refresh();
    }

    private void OnConfigurationsChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        _configurations = null;
        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        _mockServerService.StatusChanged -= OnStatusChanged;
        _mockServerService.RequestReceived -= OnRequestReceived;
        _mockServerService.ConfigurationChanged -= OnConfigurationChanged;
        _mockServerService.ConfigurationsChanged -= OnConfigurationsChanged;
    }
}
