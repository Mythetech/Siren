namespace Siren.Components.MockServer;

public class MockServerStartResult
{
    public bool Success { get; set; }

    public int? Port { get; set; }

    public string? ErrorMessage { get; set; }

    public Exception? Exception { get; set; }

    public static MockServerStartResult Succeeded(int port) => new()
    {
        Success = true,
        Port = port
    };

    public static MockServerStartResult Failed(string message, Exception? exception = null) => new()
    {
        Success = false,
        ErrorMessage = message,
        Exception = exception
    };
}
