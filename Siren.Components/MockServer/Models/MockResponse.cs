namespace Siren.Components.MockServer.Models;

public class MockResponse
{
    public int StatusCode { get; set; } = 200;

    public Dictionary<string, string> Headers { get; set; } = new()
    {
        ["Content-Type"] = "application/json"
    };

    public string Body { get; set; } = "{}";

    public int DelayMs { get; set; }
}
