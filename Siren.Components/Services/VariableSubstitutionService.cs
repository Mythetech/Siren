using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Siren.Components.Variables;

namespace Siren.Components.Services;

public partial class VariableSubstitutionService : IVariableSubstitutionService
{
    private readonly IVariableService _variableService;
    private readonly IEnumerable<IVariableValueResolver> _resolvers;
    private readonly ILogger<VariableSubstitutionService>? _logger;

    // Matches {{variableName}} or {{$dynamicVar}}
    private static readonly Regex VariablePattern = VariablePatternRegex();

    // Matches $dynamicVar patterns (for inline dynamic variables)
    private static readonly Regex DynamicVariablePattern = DynamicVariablePatternRegex();

    private const string SecretMask = "********";

    public VariableSubstitutionService(
        IVariableService variableService,
        IEnumerable<IVariableValueResolver> resolvers,
        ILogger<VariableSubstitutionService>? logger = null)
    {
        _variableService = variableService;
        _resolvers = resolvers;
        _logger = logger;
    }

    /// <inheritdoc />
    public string SubstituteVariables(string input, string? environment = null)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var variables = GetVariablesForEnvironment(environment);
        // Build dictionary with environment-specific variables taking precedence over Global
        var variableDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in variables.OrderBy(x => x.Group == VariableGroups.SystemGroup ? 0 : 1))
        {
            variableDict[v.Key] = v.Value;
        }

        return VariablePattern.Replace(input, match =>
        {
            var variableName = match.Groups[1].Value;

            // Check if it's a dynamic variable reference like {{$timestamp}}
            if (variableName.StartsWith('$'))
            {
                // For sync method, just return the raw dynamic variable - it won't be resolved
                return match.Value;
            }

            if (variableDict.TryGetValue(variableName, out var value))
            {
                // If the value is a secret reference or dynamic, return it unresolved in sync mode
                if (value.StartsWith('$'))
                {
                    return value;
                }
                return value;
            }
            return match.Value;
        });
    }

    /// <inheritdoc />
    public async Task<SubstitutionResult> SubstituteVariablesAsync(
        string input,
        string? environment = null,
        bool maskSecrets = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(input))
            return SubstitutionResult.Ok(input);

        var variables = GetVariablesForEnvironment(environment);
        // Build dictionary with environment-specific variables taking precedence over Global
        var variableDict = new Dictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in variables.OrderBy(x => x.Group == VariableGroups.SystemGroup ? 0 : 1))
        {
            variableDict[v.Key] = v;
        }

        var errors = new List<string>();
        var hasSecrets = false;
        var result = input;

        // Find all matches first, then process them
        var matches = VariablePattern.Matches(input);
        var replacements = new Dictionary<string, string>();

        foreach (Match match in matches)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var fullMatch = match.Value;
            var variableName = match.Groups[1].Value;

            // Skip if we've already processed this exact match
            if (replacements.ContainsKey(fullMatch))
                continue;

            string resolvedValue;

            // Check if it's a direct dynamic variable reference like {{$timestamp}}
            if (variableName.StartsWith('$'))
            {
                var resolution = await ResolveValueAsync(variableName, cancellationToken);
                if (!resolution.Success)
                {
                    errors.Add(resolution.ErrorMessage ?? $"Failed to resolve {variableName}");
                }
                if (resolution.IsSecret)
                {
                    hasSecrets = true;
                    resolvedValue = maskSecrets ? SecretMask : resolution.ResolvedValue;
                }
                else
                {
                    resolvedValue = resolution.ResolvedValue;
                }
            }
            // Check if it's a named variable
            else if (variableDict.TryGetValue(variableName, out var variable))
            {
                var variableValue = variable.Value;

                // Check if the variable value needs resolution (secret or dynamic)
                if (variableValue.StartsWith('$'))
                {
                    var resolution = await ResolveValueAsync(variableValue, cancellationToken);
                    if (!resolution.Success)
                    {
                        errors.Add(resolution.ErrorMessage ?? $"Failed to resolve variable '{variableName}'");
                    }
                    if (resolution.IsSecret || variable.Secret)
                    {
                        hasSecrets = true;
                        resolvedValue = maskSecrets ? SecretMask : resolution.ResolvedValue;
                    }
                    else
                    {
                        resolvedValue = resolution.ResolvedValue;
                    }
                }
                else
                {
                    // Plain value - check if variable is marked as secret
                    if (variable.Secret)
                    {
                        hasSecrets = true;
                        resolvedValue = maskSecrets ? SecretMask : variableValue;
                    }
                    else
                    {
                        resolvedValue = variableValue;
                    }
                }
            }
            else
            {
                // Variable not found - leave as is
                resolvedValue = fullMatch;
            }

            replacements[fullMatch] = resolvedValue;
        }

        // Apply all replacements
        foreach (var (placeholder, replacement) in replacements)
        {
            result = result.Replace(placeholder, replacement);
        }

        return new SubstitutionResult(result, hasSecrets, errors);
    }

    /// <summary>
    /// Resolves a value using the registered resolvers
    /// </summary>
    private async Task<VariableResolutionResult> ResolveValueAsync(
        string value,
        CancellationToken cancellationToken)
    {
        foreach (var resolver in _resolvers)
        {
            if (resolver.CanResolve(value))
            {
                try
                {
                    return await resolver.ResolveAsync(value, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Resolver {ResolverType} failed for value {Value}",
                        resolver.GetType().Name, value);
                    return VariableResolutionResult.Fail(
                        $"[ERROR:{value}]",
                        $"Resolver failed: {ex.Message}"
                    );
                }
            }
        }

        // No resolver could handle this value - return as-is
        return VariableResolutionResult.Ok(value);
    }

    private List<Variable> GetVariablesForEnvironment(string? environment)
    {
        var allVariables = _variableService.GetVariables();

        if (string.IsNullOrEmpty(environment))
        {
            return [];
        }

        return allVariables.Where(v =>
            string.Equals(v.Group, environment, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v.Group, VariableGroups.SystemGroup, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    // Matches {{variableName}} or {{$dynamicVar}} - captures the inner content
    [GeneratedRegex(@"\{\{(\$?\w+(?::\S+)?)\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePatternRegex();

    // Matches standalone $variable patterns
    [GeneratedRegex(@"\$\w+", RegexOptions.Compiled)]
    private static partial Regex DynamicVariablePatternRegex();
}
