using System;
namespace Siren.Components.Variables
{
    public interface IVariableService
    {
        void CreateVariable(Variable variable);

        void CreateVariables(List<Variable> variables);

        void UpdateVariable(Variable variable);

        void UpdateVariables(List<Variable> variables);

        void DeleteVariable(Guid id);

        void DeleteVariables(List<Guid> ids);

        List<Variable> GetVariables();

        List<VariableGroup> GetVariableGroups();
    }
}

