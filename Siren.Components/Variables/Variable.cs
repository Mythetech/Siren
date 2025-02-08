using System;
namespace Siren.Components.Variables
{


    public class Variable
    {
        /// <summary>
        /// Unique identifier of the variable
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Variable key
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// Variable value
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// Indicates if the value is a secret value and should display as a protected text field
        /// </summary>
        public bool Secret { get; set; } = false;

        /// <summary>
        /// Groups or environments the variable belongs to
        /// </summary>
        public string Group { get; set; } = VariableGroups.SystemGroup;

        public static Variable Create(string key, string value, bool isSecret = false, string group = "Global")
        {
            return new Variable()
            {
                Key = key,
                Value = value,
                Secret = isSecret,
                Group = group
            };
        }
    }
}

