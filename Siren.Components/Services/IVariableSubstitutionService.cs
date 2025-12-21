namespace Siren.Components.Services
{
    public interface IVariableSubstitutionService
    {
        string SubstituteVariables(string input, string? environment = null);
    }
}

