using Siren.Components.Variables;

namespace Siren.Test.Infrastructure;

/// <summary>
/// Fails for the value "$broken", the way the secret resolver does when a secret is missing.
/// </summary>
internal sealed class FailingVariableResolver : IVariableValueResolver
{
    public const string Value = "$broken";
    public const string Error = "Could not resolve $broken";

    public bool CanResolve(string value) => value == Value;

    public Task<VariableResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default)
        => Task.FromResult(VariableResolutionResult.Fail("[ERROR:$broken]", Error));
}
