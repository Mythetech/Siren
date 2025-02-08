using Siren.Components.Variables;
namespace Siren.Variables
{
    public class PersistentVariable : Variable
    {
        public int DocumentId { get; set; }

        public static PersistentVariable Create(Variable variable)
        {
            return new PersistentVariable()
            {
                Id = variable.Id,
                Key = variable.Key,
                Value = variable.Value,
                Secret = variable.Secret,
                Group = variable.Group
            };
        }

        public Variable ToVariable()
        {
            return this;
        }
    }
}

