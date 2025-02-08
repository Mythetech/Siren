using System;
namespace Siren.Components.Variables
{
    public class VariableGroup
    {
        public string Name { get; set; } = "";

        public List<Variable> Variables { get; set; } = new();
    }
}

