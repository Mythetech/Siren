using System.Text.RegularExpressions;
using Siren.Components.Variables;

namespace Siren.Components.Services
{
    public partial class VariableSubstitutionService : IVariableSubstitutionService
    {
        private readonly IVariableService _variableService;
        private static readonly Regex VariablePattern = VariablePatternRegex();

        public VariableSubstitutionService(IVariableService variableService)
        {
            _variableService = variableService;
        }

        public string SubstituteVariables(string input, string? environment = null)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var variables = GetVariablesForEnvironment(environment);
            var variableDict = variables.ToDictionary(v => v.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

            return VariablePattern.Replace(input, match =>
            {
                var variableName = match.Groups[1].Value;
                if (variableDict.TryGetValue(variableName, out var value))
                {
                    return value;
                }
                return match.Value;
            });
        }

        private List<Variable> GetVariablesForEnvironment(string? environment)
        {
            var allVariables = _variableService.GetVariables();

            if (string.IsNullOrEmpty(environment))
            {
                return new List<Variable>();
            }

            return allVariables.Where(v => 
                string.Equals(v.Group, environment, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.Group, VariableGroups.SystemGroup, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
        private static partial Regex VariablePatternRegex();
    }
}

