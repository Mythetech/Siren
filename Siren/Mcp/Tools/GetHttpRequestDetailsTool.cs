using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.History;

namespace Siren.Mcp.Tools;

public class GetHttpRequestDetailsInput
{
    [McpToolInput(Description = "The ID (GUID) of the history record to retrieve details for", Required = true)]
    public string Id { get; set; } = "";
}

[McpTool(Name = "get_http_request_details", Description = "Get detailed information about a specific HTTP request from history, including full request/response headers, body, timing, and network information.")]
public class GetHttpRequestDetailsTool : IMcpTool<GetHttpRequestDetailsInput>
{
    private readonly IHistoryService _historyService;

    public GetHttpRequestDetailsTool(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    public Task<McpToolResult> ExecuteAsync(GetHttpRequestDetailsInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(input.Id, out var id))
            {
                return Task.FromResult(McpToolResult.Error($"Invalid ID format: '{input.Id}'. Expected a GUID."));
            }

            var history = _historyService.GetHistory();
            var record = history.FirstOrDefault(h => h.Id == id);

            if (record == null)
            {
                return Task.FromResult(McpToolResult.Error($"History record with ID '{input.Id}' not found."));
            }

            var result = new
            {
                id = record.Id.ToString(),
                timestamp = record.Timestamp.ToString("O"),
                request = record.Request != null ? new
                {
                    method = record.Request.Method.Method,
                    url = record.Request.RequestUri,
                    headers = record.Request.Headers.ToDictionary(h => h.Key, h => h.Value),
                    queryParameters = record.Request.QueryParameters,
                    contentType = record.Request.ContentType,
                    body = record.Request.RawBody,
                    bodyType = record.Request.BodyType.ToString(),
                    timeout = record.Request.Timeout.TotalSeconds
                } : null,
                response = record.Response != null ? new
                {
                    statusCode = record.Response.StatusCode,
                    statusText = record.Response.HttpStatusCode.ToString(),
                    durationMs = record.Response.Duration.TotalMilliseconds,
                    headers = record.Response.Headers,
                    body = record.Response.ResponseText,
                    requestSize = record.Response.RequestSize != null ? new
                    {
                        body = record.Response.RequestSize.Body,
                        headers = record.Response.RequestSize.Headers
                    } : null,
                    responseSize = record.Response.ResponseSize != null ? new
                    {
                        body = record.Response.ResponseSize.Body,
                        headers = record.Response.ResponseSize.Headers
                    } : null,
                    networkInfo = record.Response.NetworkInfo != null ? new
                    {
                        localAddress = record.Response.NetworkInfo.LocalAddress,
                        remoteAddress = record.Response.NetworkInfo.RemoteAddress,
                        tlsProtocol = record.Response.NetworkInfo.TlsProtocol,
                        cipherName = record.Response.NetworkInfo.CipherName,
                        certificateCommonName = record.Response.NetworkInfo.CertificateCommonName,
                        certificateIssuer = record.Response.NetworkInfo.CertificateIssuer,
                        certificateValidUntil = record.Response.NetworkInfo.CertificateValidUntil?.ToString("O")
                    } : null,
                    timeline = record.Response.Timeline != null ? new
                    {
                        dnsLookupMs = record.Response.Timeline.DnsLookup?.TotalMilliseconds,
                        tcpConnectionMs = record.Response.Timeline.TcpConnection?.TotalMilliseconds,
                        tlsHandshakeMs = record.Response.Timeline.TlsHandshake?.TotalMilliseconds,
                        requestSentMs = record.Response.Timeline.RequestSent?.TotalMilliseconds,
                        waitingForResponseMs = record.Response.Timeline.WaitingForResponse?.TotalMilliseconds,
                        contentDownloadMs = record.Response.Timeline.ContentDownload?.TotalMilliseconds,
                        totalDurationMs = record.Response.Timeline.TotalDuration.TotalMilliseconds
                    } : null,
                    error = record.Response.Error?.Message
                } : null
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to get request details: {ex.Message}"));
        }
    }
}
