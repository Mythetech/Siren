using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp;
using Siren.Components.Variables;

namespace Siren.Mcp.Tools;

public class ListVariablesInput
{
    [McpToolInput(Description = "The name of the environment to list variables for (e.g., 'Global', 'Production')", Required = true)]
    public string Environment { get; set; } = "";
}

[McpTool(Name = "list_variables", Description = "List all variables for a specific environment. Secret values are masked for security.")]
public class ListVariablesTool : IMcpTool<ListVariablesInput>
{
    private readonly IVariableService _variableService;
    private const string MaskedValue = "********";

    public ListVariablesTool(IVariableService variableService)
    {
        _variableService = variableService;
    }

    public Task<McpToolResult> ExecuteAsync(ListVariablesInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            var environments = _variableService.GetEnvironments();
            var environment = environments.FirstOrDefault(e =>
                string.Equals(e.Name, input.Environment, StringComparison.OrdinalIgnoreCase));

            if (environment == null)
            {
                var availableEnvs = string.Join(", ", environments.Select(e => e.Name));
                return Task.FromResult(McpToolResult.Error(
                    $"Environment '{input.Environment}' not found. Available environments: {availableEnvs}"));
            }

            var result = new
            {
                environment = environment.Name,
                count = environment.Variables.Count,
                variables = environment.Variables.Select(v => new
                {
                    id = v.Id.ToString(),
                    key = v.Key,
                    value = v.Secret ? MaskedValue : v.Value,
                    isSecret = v.Secret
                }).ToList()
            };

            return Task.FromResult(McpToolResult.Text(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })));
        }
        catch (Exception ex)
        {
            return Task.FromResult(McpToolResult.Error($"Failed to list variables: {ex.Message}"));
        }
    }
}
