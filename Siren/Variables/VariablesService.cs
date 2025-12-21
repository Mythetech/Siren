using System;
using Siren.Components.Variables;

namespace Siren.Variables
{
    public class VariablesService : IVariableService
    {
        public void CreateVariable(Variable variable)
        {
            VariableRepository.CreateVariable(variable);
        }

        public void CreateVariables(List<Variable> variables)
        {
            VariableRepository.CreateVariables(variables);
        }

        public void DeleteVariable(Guid id)
        {
            VariableRepository.DeleteVariable(id);
        }

        public void DeleteVariables(List<Guid> ids)
        {
            VariableRepository.DeleteVariables(ids);
        }

        public List<Siren.Components.Variables.Environment> GetEnvironments()
        {
            var variables = GetVariables();

            var environments = variables
                .GroupBy(v => v.Group)
                .Select(g => new Siren.Components.Variables.Environment
                {
                    Name = g.Key,
                    Variables = g.ToList()
                })
                .ToList();

            if (environments.Count == 0)
            {
                environments.Add(new Siren.Components.Variables.Environment
                {
                    Name = VariableGroups.SystemGroup,
                    Variables = new List<Variable>()
                });
            }

            return environments;
        }

        public List<Variable> GetVariables()
        {
            return VariableRepository.GetVariables();
        }

        public void UpdateVariable(Variable variable)
        {
            VariableRepository.UpdateVariable(variable);
        }

        public void UpdateVariables(List<Variable> variables)
        {
            VariableRepository.UpdateVariables(variables);
        }
    }
}

