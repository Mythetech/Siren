using System;
namespace Siren.Components.Variables
{
    public class Environment
    {
        public string Name { get; set; } = "";

        public List<Variable> Variables { get; set; } = new();
    }
}

