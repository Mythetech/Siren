namespace Siren.Components.Variables;

/// <summary>
/// Metadata for a dynamic variable including its generator, description, and example output
/// </summary>
public record DynamicVariableInfo(
    string Name,
    string Description,
    Func<string> Generator,
    string ExampleOutput
);

/// <summary>
/// Resolves dynamic variables that generate values at runtime.
/// Supports: $timestamp, $isoDate, $uuid, $randomInt, $randomString
/// </summary>
public class DynamicVariableResolver : IVariableValueResolver
{
    private static readonly Dictionary<string, DynamicVariableInfo> Variables = new(StringComparer.OrdinalIgnoreCase)
    {
        ["$uuid"] = new DynamicVariableInfo(
            "$uuid",
            "Random UUID v4",
            () => Guid.NewGuid().ToString(),
            "550e8400-e29b-41d4-a716-446655440000"
        ),
        ["$timestamp"] = new DynamicVariableInfo(
            "$timestamp",
            "Unix timestamp (seconds)",
            () => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            "1704067200"
        ),
        ["$isoDate"] = new DynamicVariableInfo(
            "$isoDate",
            "ISO 8601 date/time",
            () => DateTime.UtcNow.ToString("O"),
            "2024-01-01T00:00:00.0000000Z"
        ),
        ["$randomInt"] = new DynamicVariableInfo(
            "$randomInt",
            "Random integer",
            () => Random.Shared.Next(0, int.MaxValue).ToString(),
            "1847293654"
        ),
        ["$randomString"] = new DynamicVariableInfo(
            "$randomString",
            "Random 16-char string",
            () => Guid.NewGuid().ToString("N")[..16],
            "a1b2c3d4e5f6g7h8"
        )
    };

    /// <summary>
    /// Gets the list of supported dynamic variable names
    /// </summary>
    public static IReadOnlyCollection<string> SupportedVariables => Variables.Keys;

    /// <summary>
    /// Gets metadata for all supported dynamic variables
    /// </summary>
    public static IReadOnlyCollection<DynamicVariableInfo> GetVariableInfos() => Variables.Values;

    /// <inheritdoc />
    public bool CanResolve(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.StartsWith('$') && Variables.ContainsKey(value);
    }

    /// <inheritdoc />
    public Task<VariableResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default)
    {
        if (Variables.TryGetValue(value, out var info))
        {
            try
            {
                var resolvedValue = info.Generator();
                return Task.FromResult(VariableResolutionResult.Ok(resolvedValue));
            }
            catch (Exception ex)
            {
                return Task.FromResult(VariableResolutionResult.Fail(
                    $"[DYNAMIC_ERROR:{value}]",
                    $"Failed to generate dynamic value for '{value}': {ex.Message}"
                ));
            }
        }

        return Task.FromResult(VariableResolutionResult.Fail(
            value,
            $"Unknown dynamic variable: {value}. Supported: {string.Join(", ", Variables.Keys)}"
        ));
    }
}
