using System;
using Siren.Components.Collections;

namespace Siren.Collections
{
    public class PersistentCollection : Collection
    {
        public int DocumentId { get; set; }

        public static PersistentCollection Create(Collection collection)
        {
            return new PersistentCollection()
            {
                Id = collection.Id,
                Name = collection.Name,
                Requests = collection?.Requests
            };
        }

        public Collection ToCollection()
        {
            return this;
        }
    }
}

