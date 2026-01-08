using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.Variables;

namespace Siren.Mcp.Tools;

[McpTool(Name = "list_variable_environments", Description = "List all variable environments in Siren. Each environment contains variables that can be used in HTTP requests.")]
public class ListVariableEnvironmentsTool : IMcpTool
{
    private readonly IVariableService _variableService;

    public ListVariableEnvironmentsTool(IVariableService variableService)
    {
        _variableService = variableService;
    }

    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        try
        {
            var environments = _variableService.GetEnvironments();

            var result = new
            {
                count = environments.Count,
                environments = environments.Select(e => new
                {
                    name = e.Name,
                    variableCount = e.Variables.Count
                }).ToList()
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to list environments: {ex.Message}"));
        }
    }
}
