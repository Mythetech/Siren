using System;
namespace Siren.Components.Collections
{
    public interface IOpenApiImporter
    {
        public Task<Collection> ImportFromOpenApiSpec(string specUrl);
    }
}

