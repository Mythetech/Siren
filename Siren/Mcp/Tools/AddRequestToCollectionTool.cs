using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.Collections;
using Siren.Components.Http.Models;

namespace Siren.Mcp.Tools;

public class AddRequestToCollectionInput
{
    [McpToolInput(Description = "The ID (GUID) of the collection to add the request to", Required = true)]
    public string CollectionId { get; set; } = "";

    [McpToolInput(Description = "HTTP method (GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS)", Required = true)]
    public string Method { get; set; } = "GET";

    [McpToolInput(Description = "The full URL for the request", Required = true)]
    public string Url { get; set; } = "";

    [McpToolInput(Description = "Request headers as key-value pairs (JSON object)", Required = false)]
    public Dictionary<string, string>? Headers { get; set; }

    [McpToolInput(Description = "Request body content (for POST, PUT, PATCH requests)", Required = false)]
    public string? Body { get; set; }

    [McpToolInput(Description = "Content type of the body (default: application/json)", Required = false)]
    public string? ContentType { get; set; }
}

[McpTool(Name = "add_request_to_collection", Description = "Add a new HTTP request to an existing collection in Siren.")]
public class AddRequestToCollectionTool : IMcpTool<AddRequestToCollectionInput>
{
    private readonly ICollectionsService _collectionsService;

    public AddRequestToCollectionTool(ICollectionsService collectionsService)
    {
        _collectionsService = collectionsService;
    }

    public Task<McpToolResult> ExecuteAsync(AddRequestToCollectionInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(input.CollectionId, out var collectionId))
            {
                return Task.FromResult(McpToolResult.Error($"Invalid collection ID format: '{input.CollectionId}'. Expected a GUID."));
            }

            var collection = _collectionsService.GetCollection(collectionId);
            if (collection == null)
            {
                return Task.FromResult(McpToolResult.Error($"Collection with ID '{input.CollectionId}' not found."));
            }

            var httpMethod = new HttpMethod(input.Method.ToUpperInvariant());

            var request = new HttpRequest
            {
                Id = Guid.NewGuid(),
                Method = httpMethod,
                RequestUri = input.Url,
                DisplayUri = input.Url,
                ContentType = input.ContentType ?? "application/json",
                RawBody = input.Body ?? "",
                BodyType = string.IsNullOrEmpty(input.Body) ? RequestBodyType.None : RequestBodyType.Raw,
                Headers = input.Headers?.Select(h => new KeyValuePair<string, string>(h.Key, h.Value)).ToList()
                    ?? new List<KeyValuePair<string, string>>()
            };

            _collectionsService.AddRequestToCollection(collectionId, request);

            var result = new
            {
                success = true,
                message = $"Request added to collection '{collection.Name}'",
                requestId = request.Id.ToString(),
                collectionId = collectionId.ToString(),
                collectionName = collection.Name
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to add request to collection: {ex.Message}"));
        }
    }
}
