using System.Text;
using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.Http.Models;
using Siren.Components.Services;

namespace Siren.Mcp.Tools;

public class SendHttpRequestInput
{
    [McpToolInput(Description = "HTTP method (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS)", Required = true)]
    public string Method { get; set; } = "GET";

    [McpToolInput(Description = "The full URL to send the request to", Required = true)]
    public string Url { get; set; } = "";

    [McpToolInput(Description = "Request headers as key-value pairs (JSON object)", Required = false)]
    public Dictionary<string, string>? Headers { get; set; }

    [McpToolInput(Description = "Query parameters as key-value pairs (JSON object)", Required = false)]
    public Dictionary<string, string>? QueryParameters { get; set; }

    [McpToolInput(Description = "Request body content (for POST, PUT, PATCH requests)", Required = false)]
    public string? Body { get; set; }

    [McpToolInput(Description = "Content type of the body (default: application/json)", Required = false)]
    public string? ContentType { get; set; }

    [McpToolInput(Description = "Request timeout in seconds (default: 30)", Required = false)]
    public int? TimeoutSeconds { get; set; }
}

[McpTool(Name = "send_http_request", Description = "Send an HTTP request and return the response. Supports all HTTP methods, custom headers, query parameters, and request bodies.")]
public class SendHttpRequestTool : IMcpTool<SendHttpRequestInput>
{
    private readonly IHttpRequestClient _httpRequestClient;

    public SendHttpRequestTool(IHttpRequestClient httpRequestClient)
    {
        _httpRequestClient = httpRequestClient;
    }

    public async Task<McpToolResult> ExecuteAsync(SendHttpRequestInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpMethod = new HttpMethod(input.Method.ToUpperInvariant());

            var url = input.Url;
            if (input.QueryParameters != null && input.QueryParameters.Count > 0)
            {
                var queryString = string.Join("&", input.QueryParameters.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

                url = url.Contains('?')
                    ? $"{url}&{queryString}"
                    : $"{url}?{queryString}";
            }

            var contentType = input.ContentType ?? "application/json";

            var request = new HttpRequest
            {
                Id = Guid.NewGuid(),
                Method = httpMethod,
                RequestUri = url,
                DisplayUri = url,
                ContentType = contentType,
                RawBody = input.Body ?? "",
                BodyType = string.IsNullOrEmpty(input.Body) ? RequestBodyType.None : RequestBodyType.Raw,
                Timeout = TimeSpan.FromSeconds(input.TimeoutSeconds ?? 30),
                Headers = input.Headers?.Select(h => new KeyValuePair<string, string>(h.Key, h.Value)).ToList()
                    ?? new List<KeyValuePair<string, string>>()
            };

            if (!string.IsNullOrEmpty(input.Body))
            {
                request.Content = new StringContent(input.Body, Encoding.UTF8, contentType);
            }

            var response = await _httpRequestClient.SendHttpRequestAsync(request, cancellationToken);

            var result = new
            {
                statusCode = response.StatusCode,
                statusText = response.HttpStatusCode.ToString(),
                durationMs = response.Duration.TotalMilliseconds,
                headers = response.Headers,
                body = response.ResponseText,
                error = response.Error?.Message,
                requestSize = response.RequestSize != null ? new
                {
                    body = response.RequestSize.Body,
                    headers = response.RequestSize.Headers
                } : null,
                responseSize = response.ResponseSize != null ? new
                {
                    body = response.ResponseSize.Body,
                    headers = response.ResponseSize.Headers
                } : null,
                networkInfo = response.NetworkInfo != null ? new
                {
                    localAddress = response.NetworkInfo.LocalAddress,
                    remoteAddress = response.NetworkInfo.RemoteAddress,
                    tlsProtocol = response.NetworkInfo.TlsProtocol
                } : null
            };

            return McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return McpToolResult.Error($"Failed to send HTTP request: {ex.Message}");
        }
    }
}
