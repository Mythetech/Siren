using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Siren.Components.MockServer;
using Siren.Components.MockServer.Models;
using Siren.Components.Services;

namespace Siren.MockServer;

public class MockServerService : IMockServerService, IDisposable
{
    private readonly ILogger<MockServerService> _logger;
    private readonly IVariableSubstitutionService _variableSubstitution;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private MockServerConfiguration? _activeConfig;
    private readonly List<MockRequestLog> _requestLogs = new();
    private readonly object _lock = new();

    private const int DefaultPort = 9090;
    private const int PortRangeStart = 9090;
    private const int PortRangeEnd = 9100;
    private const int MaxLogEntries = 100;

    public MockServerStatus Status { get; private set; } = MockServerStatus.Stopped;
    public int? ActualPort { get; private set; }
    public string? BaseUrl => ActualPort.HasValue ? $"http://localhost:{ActualPort}" : null;
    public Guid? ActiveConfigurationId => _activeConfig?.Id;

    public event Action<MockServerStatus>? StatusChanged;
    public event Action<MockRequestLog>? RequestReceived;
    public event Action<MockServerConfiguration>? ConfigurationChanged;
    public event Action? ConfigurationsChanged;

    public MockServerService(ILogger<MockServerService> logger, IVariableSubstitutionService variableSubstitution)
    {
        _logger = logger;
        _variableSubstitution = variableSubstitution;
    }

    public async Task<MockServerStartResult> StartAsync(Guid? configurationId = null)
    {
        if (Status == MockServerStatus.Running)
        {
            return MockServerStartResult.Succeeded(ActualPort!.Value);
        }

        try
        {
            SetStatus(MockServerStatus.Starting);

            if (configurationId.HasValue)
            {
                _activeConfig = MockServerRepository.GetConfiguration(configurationId.Value);
            }
            else
            {
                _activeConfig = MockServerRepository.GetConfigurations().FirstOrDefault();
            }

            if (_activeConfig == null)
            {
                _activeConfig = CreateConfiguration("Default Mock Server");
            }

            var port = FindAvailablePort(_activeConfig.Port);
            if (!port.HasValue)
            {
                SetStatus(MockServerStatus.Error);
                return MockServerStartResult.Failed($"No available ports in range {PortRangeStart}-{PortRangeEnd}");
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port.Value}/");

            _listener.Start();
            ActualPort = port.Value;

            _cts = new CancellationTokenSource();
            _listenerTask = ListenAsync(_cts.Token);

            SetStatus(MockServerStatus.Running);
            _logger.LogInformation("Mock server started on port {Port}", port.Value);

            return MockServerStartResult.Succeeded(port.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start mock server");
            SetStatus(MockServerStatus.Error);
            return MockServerStartResult.Failed(ex.Message, ex);
        }
    }

    public async Task StopAsync()
    {
        if (Status != MockServerStatus.Running && Status != MockServerStatus.Starting)
            return;

        SetStatus(MockServerStatus.Stopping);

        try
        {
            _cts?.Cancel();

            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
                _listener = null;
            }

            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }

            ActualPort = null;
            SetStatus(MockServerStatus.Stopped);
            _logger.LogInformation("Mock server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping mock server");
            SetStatus(MockServerStatus.Error);
        }
    }

    public async Task RestartAsync()
    {
        var configId = _activeConfig?.Id;
        await StopAsync();
        await StartAsync(configId);
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                _ = ProcessRequestAsync(context);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var response = context.Response;

        var log = new MockRequestLog
        {
            Method = request.HttpMethod,
            Path = request.Url?.AbsolutePath ?? "/",
            QueryString = request.Url?.Query ?? "",
            Headers = request.Headers.AllKeys
                .Where(k => k != null)
                .ToDictionary(k => k!, k => request.Headers[k] ?? "")
        };

        try
        {
            // Read request body if present
            if (request.HasEntityBody)
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                log.Body = await reader.ReadToEndAsync();
            }

            var endpoint = FindMatchingEndpoint(request.HttpMethod, log.Path);

            if (endpoint != null)
            {
                log.MatchedEndpointId = endpoint.Id;

                if (endpoint.Response.DelayMs > 0)
                {
                    await Task.Delay(endpoint.Response.DelayMs);
                }

                response.StatusCode = endpoint.Response.StatusCode;
                log.ResponseStatusCode = endpoint.Response.StatusCode;

                foreach (var header in endpoint.Response.Headers)
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        response.ContentType = header.Value;
                    }
                    else
                    {
                        response.Headers[header.Key] = header.Value;
                    }
                }

                var body = _variableSubstitution.SubstituteVariables(endpoint.Response.Body);
                var bodyBytes = Encoding.UTF8.GetBytes(body);
                response.ContentLength64 = bodyBytes.Length;
                await response.OutputStream.WriteAsync(bodyBytes);
            }
            else
            {
                response.StatusCode = 404;
                log.ResponseStatusCode = 404;
                response.ContentType = "application/json";

                var errorBody = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "No matching mock endpoint",
                    path = log.Path,
                    method = log.Method
                });

                var errorBytes = Encoding.UTF8.GetBytes(errorBody);
                response.ContentLength64 = errorBytes.Length;
                await response.OutputStream.WriteAsync(errorBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mock request");
            response.StatusCode = 500;
            log.ResponseStatusCode = 500;
        }
        finally
        {
            stopwatch.Stop();
            log.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            response.Close();

            AddRequestLog(log);
        }
    }

    private MockEndpoint? FindMatchingEndpoint(string method, string path)
    {
        if (_activeConfig == null) return null;

        return _activeConfig.Endpoints
            .Where(e => e.IsEnabled)
            .OrderByDescending(e => e.Priority)
            .ThenByDescending(e => e.RoutePattern.Length) // More specific routes first
            .FirstOrDefault(e => MatchesEndpoint(e, method, path));
    }

    private bool MatchesEndpoint(MockEndpoint endpoint, string method, string path)
    {
        if (endpoint.Method != "*" &&
            !endpoint.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return MatchRoute(endpoint.RoutePattern, path);
    }

    private bool MatchRoute(string pattern, string path)
    {
        pattern = pattern.Trim('/');
        path = path.Trim('/');

        if (pattern.Equals(path, StringComparison.OrdinalIgnoreCase))
            return true;

        var patternParts = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (patternParts.Length > 0 && patternParts[^1] == "*")
        {
            for (int i = 0; i < patternParts.Length - 1; i++)
            {
                if (i >= pathParts.Length) return false;

                var patternPart = patternParts[i];
                if (patternPart.StartsWith('{') && patternPart.EndsWith('}'))
                    continue; // Path parameter - matches anything

                if (!patternPart.Equals(pathParts[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        if (patternParts.Length != pathParts.Length)
            return false;

        for (int i = 0; i < patternParts.Length; i++)
        {
            var patternPart = patternParts[i];

            // Path parameter (e.g., {id})
            if (patternPart.StartsWith('{') && patternPart.EndsWith('}'))
                continue;

            // Exact match required
            if (!patternPart.Equals(pathParts[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private void AddRequestLog(MockRequestLog log)
    {
        lock (_lock)
        {
            _requestLogs.Insert(0, log);
            if (_requestLogs.Count > MaxLogEntries)
            {
                _requestLogs.RemoveAt(_requestLogs.Count - 1);
            }
        }

        RequestReceived?.Invoke(log);
    }

    private int? FindAvailablePort(int preferredPort)
    {
        for (int port = preferredPort; port <= PortRangeEnd; port++)
        {
            try
            {
                using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch
            {
                continue;
            }
        }

        if (preferredPort > PortRangeStart)
        {
            for (int port = PortRangeStart; port < preferredPort; port++)
            {
                try
                {
                    using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch
                {
                    continue;
                }
            }
        }

        return null;
    }

    private void SetStatus(MockServerStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(status);
    }

    #region Configuration Management

    public MockServerConfiguration? GetActiveConfiguration() => _activeConfig;

    public List<MockServerConfiguration> GetConfigurations() =>
        MockServerRepository.GetConfigurations();

    public MockServerConfiguration? GetConfiguration(Guid id) =>
        MockServerRepository.GetConfiguration(id);

    public void SaveConfiguration(MockServerConfiguration configuration)
    {
        configuration.ModifiedAt = DateTimeOffset.UtcNow;
        MockServerRepository.UpsertConfiguration(configuration);

        if (_activeConfig?.Id == configuration.Id)
        {
            _activeConfig = configuration;
        }

        ConfigurationChanged?.Invoke(configuration);
        ConfigurationsChanged?.Invoke();
    }

    public void DeleteConfiguration(Guid id)
    {
        MockServerRepository.DeleteConfiguration(id);

        if (_activeConfig?.Id == id)
        {
            _activeConfig = GetConfigurations().FirstOrDefault();
        }

        ConfigurationsChanged?.Invoke();
    }

    public void SetActiveConfiguration(Guid id)
    {
        var config = MockServerRepository.GetConfiguration(id);
        if (config != null)
        {
            _activeConfig = config;
            ConfigurationChanged?.Invoke(config);
        }
    }

    public MockServerConfiguration CreateConfiguration(string name)
    {
        var config = new MockServerConfiguration
        {
            Name = name,
            Port = DefaultPort
        };

        MockServerRepository.UpsertConfiguration(config);
        ConfigurationsChanged?.Invoke();

        return config;
    }

    #endregion

    #region Endpoint Management

    public void AddEndpoint(Guid configurationId, MockEndpoint endpoint)
    {
        var config = GetConfiguration(configurationId);
        if (config == null) return;

        config.Endpoints.Add(endpoint);
        SaveConfiguration(config);
    }

    public void UpdateEndpoint(Guid configurationId, MockEndpoint endpoint)
    {
        var config = GetConfiguration(configurationId);
        if (config == null) return;

        var index = config.Endpoints.FindIndex(e => e.Id == endpoint.Id);
        if (index >= 0)
        {
            config.Endpoints[index] = endpoint;
            SaveConfiguration(config);
        }
    }

    public void RemoveEndpoint(Guid configurationId, Guid endpointId)
    {
        var config = GetConfiguration(configurationId);
        if (config == null) return;

        config.Endpoints.RemoveAll(e => e.Id == endpointId);
        SaveConfiguration(config);
    }

    public MockEndpoint? GetEndpoint(Guid configurationId, Guid endpointId)
    {
        var config = GetConfiguration(configurationId);
        return config?.Endpoints.FirstOrDefault(e => e.Id == endpointId);
    }

    #endregion

    #region Request Logging

    public IReadOnlyList<MockRequestLog> GetRequestLogs()
    {
        lock (_lock)
        {
            return _requestLogs.ToList().AsReadOnly();
        }
    }

    public void ClearRequestLogs()
    {
        lock (_lock)
        {
            _requestLogs.Clear();
        }
    }

    #endregion

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Close();
        _cts?.Dispose();
    }
}
