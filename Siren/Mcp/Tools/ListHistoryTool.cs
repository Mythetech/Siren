using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.History;

namespace Siren.Mcp.Tools;

public class ListHistoryInput
{
    [McpToolInput(Description = "Maximum number of history records to return (default: 50)", Required = false)]
    public int? Limit { get; set; }
}

[McpTool(Name = "list_history", Description = "List HTTP request history records from Siren. Returns the most recent requests with their status codes and timestamps.")]
public class ListHistoryTool : IMcpTool<ListHistoryInput>
{
    private readonly IHistoryService _historyService;

    public ListHistoryTool(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    public Task<McpToolResult> ExecuteAsync(ListHistoryInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = _historyService.GetHistory();
            var limit = input?.Limit ?? 50;

            var records = history
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .Select(h => new
                {
                    id = h.Id.ToString(),
                    timestamp = h.Timestamp.ToString("O"),
                    method = h.HttpMethod.Method,
                    url = h.RequestUri,
                    statusCode = (int)h.StatusCode
                })
                .ToList();

            var result = new
            {
                count = records.Count,
                totalRecords = history.Count,
                records
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to list history: {ex.Message}"));
        }
    }
}
