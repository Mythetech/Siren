using System;
using LiteDB;
using Microsoft.Extensions.Logging;
using Siren.Components.Collections;
using Siren.Components.Http;
using Siren.Components.Http.Models;

namespace Siren.Collections
{
    public class CollectionsService : ICollectionsService
    {
        private readonly ILogger<CollectionsService> _logger;
        private readonly IHttpClientFactory _factory;

        public CollectionsService(ILogger<CollectionsService> logger, IHttpClientFactory factory)
        {
            _logger = logger;
            _factory = factory;
        }

        private List<Collection>? _collections;

        public ILogger<CollectionsService> Logger => _logger;

        public event Action<List<Collection>>? CollectionsChanged;

        private List<Collection> LoadCollections()
        {
            if (_collections != null)
                return _collections;

            return _collections = CollectionsRepository.GetCollections();
        }

        private void AddCollection(Collection collection)
        {
            _collections ??= LoadCollections();

            try
            {
                _collections.Add(collection);

                CollectionsRepository.UpsertCollection(collection);

                CollectionsChanged?.Invoke(_collections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting collection");
            }

            _logger.LogInformation("Added collection");
        }

        public List<Collection> GetCollections()
        {
            _collections ??= LoadCollections();

            return _collections.OrderByDescending(x => x.Id).ToList();
        }

        public Collection CreateCollection(string name, HttpRequest[]? requests)
        {
            var collection = Collection.Create(name, requests);

            AddCollection(collection);

            return collection;
        }

        public void AddRequestToCollection(Guid id, HttpRequest req)
        {
            var collection = _collections.FirstOrDefault(c => c.Id == id);

            if (collection != null)
            {
                collection.Requests ??= new();
                collection.Requests.Add(req);
                CollectionsRepository.UpsertCollection(collection);
            }
        }

        public void RemoveRequestFromCollection(Guid id, Guid requestId)
        {
            if (_collections == null)
            {
                _logger.LogWarning("Tried to remove collection but no collections exist, {CollectionId}", id);
                return;
            }

            var collection = _collections.FirstOrDefault(c => c.Id == id);

            if (collection == null)
            {
                _logger.LogError("Collection does not exist {CollectionId}", id);
            }

            if (collection != null && collection?.Requests?.Count > 0)
            {
                var request = collection.Requests.FirstOrDefault(r => r.Id == requestId);

                if (request != null)
                {
                    collection.Requests.Remove(request);
                    CollectionsRepository.UpsertCollection(collection);
                }
            }
        }

        public Collection GetCollection(Guid id)
        {
            return _collections.FirstOrDefault(c => c.Id == id);
        }

        public void DeleteCollection(Guid id)
        {
            var collection = _collections.FirstOrDefault(c => c.Id == id);

            if (collection != null)
            {
                _collections.Remove(collection);
                CollectionsRepository.DeleteCollection(id);
            }
        }

        public void DeleteCollections()
        {
            _collections = new();

            CollectionsRepository.DeleteCollections();
        }

        public void UpdateCollection(Collection collection)
        {
            var existingCollection = _collections.FirstOrDefault(c => c.Id == collection.Id);

            if (existingCollection != null)
            {
                existingCollection.Name = collection.Name;
                existingCollection.Requests = collection.Requests;

                CollectionsRepository.UpsertCollection(existingCollection);

                CollectionsChanged?.Invoke(_collections);
            }
        }

        public async Task<Collection> ImportFromOpenApiSpec(string specUrl)
        {
            // Fetch the OpenAPI spec
            var httpClient = _factory.CreateClient();
            var spec = await httpClient.GetStringAsync(specUrl);

            // Parse the spec
            var openApiDocument = new Microsoft.OpenApi.Readers.OpenApiStringReader().Read(spec, out var diagnostics);

            // Get the base URL from the specUrl
            var baseUri = new Uri(specUrl);
            var baseUrl = $"{baseUri.Scheme}://{baseUri.Host}";

            if (baseUri.Port != 80 && baseUri.Port != 443)
            {
                baseUrl += $":{baseUri.Port}";
            }

            List<HttpRequest> requests = new();
            // Iterate over the paths in the spec
            foreach (var pathItem in openApiDocument.Paths)
            {
                // Iterate over the operations in each path
                foreach (var operation in pathItem.Value.Operations)
                {
                    Uri fullUri = new Uri(new Uri(baseUrl), pathItem.Key);

                    var httpRequest = new HttpRequest
                    {
                        Method = new HttpMethod(operation.Key.ToString()),
                        RequestUri = fullUri.ToString(),
                        DisplayUri = pathItem.Key,
                    };

                    // Add the HttpRequest to the collection
                    requests.Add(httpRequest);
                }
            }

            var collection = Collection.Create(specUrl.Replace("http://", "").Replace("https://", ""), requests.ToArray());

            return collection;
        }
    }
}


