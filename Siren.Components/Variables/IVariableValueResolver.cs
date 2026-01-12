namespace Siren.Components.Variables;

/// <summary>
/// Interface for resolving variable values that require special processing
/// (e.g., secret references, dynamic values)
/// </summary>
public interface IVariableValueResolver
{
    /// <summary>
    /// Determines if this resolver can handle the given value
    /// </summary>
    /// <param name="value">The variable value to check</param>
    /// <returns>True if this resolver can process the value</returns>
    bool CanResolve(string value);

    /// <summary>
    /// Resolves the value to its actual content
    /// </summary>
    /// <param name="value">The variable value to resolve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resolution result containing the resolved value or error information</returns>
    Task<VariableResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a variable value resolution attempt
/// </summary>
/// <param name="Success">Whether the resolution succeeded</param>
/// <param name="ResolvedValue">The resolved value (or error placeholder if failed)</param>
/// <param name="ErrorMessage">Error message if resolution failed</param>
/// <param name="IsSecret">Whether the resolved value is a secret (for masking purposes)</param>
public record VariableResolutionResult(
    bool Success,
    string ResolvedValue,
    string? ErrorMessage = null,
    bool IsSecret = false
)
{
    /// <summary>
    /// Creates a successful resolution result
    /// </summary>
    public static VariableResolutionResult Ok(string value, bool isSecret = false)
        => new(true, value, null, isSecret);

    /// <summary>
    /// Creates a failed resolution result
    /// </summary>
    public static VariableResolutionResult Fail(string errorPlaceholder, string errorMessage)
        => new(false, errorPlaceholder, errorMessage, false);
}
