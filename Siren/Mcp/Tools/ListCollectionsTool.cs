using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.Collections;

namespace Siren.Mcp.Tools;

[McpTool(Name = "list_collections", Description = "List all saved request collections in Siren. Collections organize related HTTP requests together.")]
public class ListCollectionsTool : IMcpTool
{
    private readonly ICollectionsService _collectionsService;

    public ListCollectionsTool(ICollectionsService collectionsService)
    {
        _collectionsService = collectionsService;
    }

    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = _collectionsService.GetCollections();

            var result = new
            {
                count = collections.Count,
                collections = collections.Select(c => new
                {
                    id = c.Id.ToString(),
                    name = c.Name,
                    requestCount = c.Requests?.Count ?? 0
                }).ToList()
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to list collections: {ex.Message}"));
        }
    }
}
