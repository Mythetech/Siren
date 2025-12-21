using System;
using System.Collections.Generic;

namespace Siren.Components.Variables
{
    public class VariableState
    {
        private readonly IVariableService _variableService;
        private List<Variable>? _variables;
        private List<Environment>? _environments;

        public VariableState(IVariableService variableService)
        {
            _variableService = variableService;
        }

        public List<Variable> Variables
        {
            get
            {
                _variables ??= _variableService.GetVariables();
                return _variables;
            }
            private set => _variables = value;
        }

        public List<Environment> Environments
        {
            get
            {
                _environments ??= _variableService.GetEnvironments();
                return _environments;
            }
            private set => _environments = value;
        }

        public event Action? StateChanged;

        public void CreateVariable(Variable variable)
        {
            _variableService.CreateVariable(variable);
            Refresh();
        }

        public void CreateVariables(List<Variable> variables)
        {
            _variableService.CreateVariables(variables);
            Refresh();
        }

        public void UpdateVariable(Variable variable)
        {
            _variableService.UpdateVariable(variable);
            Refresh();
        }

        public void UpdateVariables(List<Variable> variables)
        {
            _variableService.UpdateVariables(variables);
            Refresh();
        }

        public void DeleteVariable(Guid id)
        {
            _variableService.DeleteVariable(id);
            Refresh();
        }

        public void DeleteVariables(List<Guid> ids)
        {
            _variableService.DeleteVariables(ids);
            Refresh();
        }

        private void Refresh()
        {
            _variables = null;
            _environments = null;
            StateChanged?.Invoke();
        }
    }
}

