using System;
using Siren.Components.Http.Models;

namespace Siren.Components.Collections
{
    public class Collection
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public List<HttpRequest>? Requests { get; set; }

        public static Collection Create(string name, HttpRequest[]? requests)
        {
            return new Collection()
            {
                Name = name,
                Requests = requests?.ToList(),
                Id = Guid.NewGuid(),
            };
        }
    }
}

