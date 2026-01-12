using Mythetech.Framework.Infrastructure.Secrets;

namespace Siren.Components.Variables;

/// <summary>
/// Resolves secret references to actual secret values from the secret manager.
/// Pattern: $secret:keyname
/// </summary>
public class SecretReferenceResolver : IVariableValueResolver
{
    private const string SecretPrefix = "$secret:";
    private readonly SecretManagerState _secretManagerState;

    public SecretReferenceResolver(SecretManagerState secretManagerState)
    {
        _secretManagerState = secretManagerState ?? throw new ArgumentNullException(nameof(secretManagerState));
    }

    /// <inheritdoc />
    public bool CanResolve(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.StartsWith(SecretPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the secret key from a secret reference value
    /// </summary>
    /// <param name="value">The full value (e.g., "$secret:my-api-key")</param>
    /// <returns>The secret key (e.g., "my-api-key")</returns>
    public static string ExtractSecretKey(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(SecretPrefix, StringComparison.OrdinalIgnoreCase))
            return value;

        return value[SecretPrefix.Length..];
    }

    /// <summary>
    /// Creates a secret reference value from a key
    /// </summary>
    /// <param name="secretKey">The secret key</param>
    /// <returns>The secret reference value (e.g., "$secret:my-api-key")</returns>
    public static string CreateSecretReference(string secretKey)
    {
        return $"{SecretPrefix}{secretKey}";
    }

    /// <inheritdoc />
    public async Task<VariableResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default)
    {
        var secretKey = ExtractSecretKey(value);

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return VariableResolutionResult.Fail(
                "[SECRET_ERROR:empty_key]",
                "Secret reference has an empty key"
            );
        }

        if (!_secretManagerState.HasActiveManager)
        {
            return VariableResolutionResult.Fail(
                $"[SECRET_ERROR:{secretKey}]",
                "No secret manager is configured. Please configure a secret manager in settings."
            );
        }

        try
        {
            var result = await _secretManagerState.GetSecretAsync(secretKey);

            if (result.Success && result.Value != null)
            {
                return VariableResolutionResult.Ok(result.Value.Value, isSecret: true);
            }

            var errorMessage = result.ErrorMessage ?? $"Secret '{secretKey}' not found";
            return VariableResolutionResult.Fail(
                $"[SECRET_ERROR:{secretKey}]",
                errorMessage
            );
        }
        catch (Exception ex)
        {
            return VariableResolutionResult.Fail(
                $"[SECRET_ERROR:{secretKey}]",
                $"Failed to retrieve secret '{secretKey}': {ex.Message}"
            );
        }
    }
}
