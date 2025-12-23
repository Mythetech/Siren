using System;
using Siren.Components.Http.Models;

namespace Siren.Components.Collections
{
    public interface ICollectionsService : IOpenApiImporter
    {
        public Collection CreateCollection(string name, HttpRequest[]? requests);

        public void AddRequestToCollection(Guid id, HttpRequest req);

        public void RemoveRequestFromCollection(Guid id, Guid requestId);

        public List<Collection> GetCollections();

        public Collection GetCollection(Guid id);

        public void DeleteCollection(Guid id);

        public void DeleteCollections();

        public void UpdateCollection(Collection collection);
    }
}

