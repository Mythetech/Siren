namespace Siren.Components.Services;

public interface IVariableSubstitutionService
{
    /// <summary>
    /// Substitutes variables in the input string synchronously.
    /// This method only handles plain variable values - secrets and dynamic variables
    /// will not be resolved and will remain as their raw values.
    /// </summary>
    /// <param name="input">The input string containing {{variable}} placeholders</param>
    /// <param name="environment">The environment to use for variable resolution</param>
    /// <returns>The input with variables substituted</returns>
    string SubstituteVariables(string input, string? environment = null);

    /// <summary>
    /// Substitutes variables in the input string asynchronously, including secrets and dynamic variables.
    /// </summary>
    /// <param name="input">The input string containing {{variable}} placeholders</param>
    /// <param name="environment">The environment to use for variable resolution</param>
    /// <param name="maskSecrets">If true, secret values will be masked as ********</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The substitution result containing the resolved string and any errors</returns>
    Task<SubstitutionResult> SubstituteVariablesAsync(
        string input,
        string? environment = null,
        bool maskSecrets = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a variable substitution operation
/// </summary>
/// <param name="Value">The resulting string with variables substituted</param>
/// <param name="HasSecrets">Whether any secret values were resolved</param>
/// <param name="Errors">List of any resolution errors that occurred</param>
public record SubstitutionResult(
    string Value,
    bool HasSecrets,
    List<string> Errors
)
{
    /// <summary>
    /// Creates a successful result with no secrets
    /// </summary>
    public static SubstitutionResult Ok(string value) => new(value, false, []);

    /// <summary>
    /// Whether any errors occurred during substitution
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}
