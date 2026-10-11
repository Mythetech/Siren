using Siren.Components.Collections;
using Siren.Components.Http.Models;

namespace Siren.Test.Infrastructure;

/// <summary>
/// In-memory stand-in for the LiteDB-backed service. It raises <see cref="CollectionsChanged"/>
/// on the same operations the real service does, so components are tested against the real contract.
/// </summary>
public sealed class FakeCollectionsService : ICollectionsService
{
    private readonly List<Collection> _collections = new();

    public event Action<List<Collection>>? CollectionsChanged;

    public int SubscriberCount => CollectionsChanged?.GetInvocationList().Length ?? 0;

    public Collection? OpenApiImportResult { get; set; }

    public Exception? CreateFailure { get; set; }

    public Collection CreateCollection(string name, HttpRequest[]? requests)
    {
        if (CreateFailure != null)
            throw CreateFailure;

        var collection = Collection.Create(name, requests);
        _collections.Add(collection);
        CollectionsChanged?.Invoke(_collections);

        return collection;
    }

    public void AddRequestToCollection(Guid id, HttpRequest req)
    {
        var collection = GetCollection(id);
        collection.Requests ??= new();
        collection.Requests.Add(req);
    }

    public void RemoveRequestFromCollection(Guid id, Guid requestId)
        => GetCollection(id).Requests?.RemoveAll(r => r.Id == requestId);

    public List<Collection> GetCollections() => _collections.ToList();

    public Collection GetCollection(Guid id) => _collections.First(c => c.Id == id);

    public void DeleteCollection(Guid id) => _collections.RemoveAll(c => c.Id == id);

    public void DeleteCollections() => _collections.Clear();

    public void UpdateCollection(Collection collection)
    {
        var existing = GetCollection(collection.Id);
        existing.Name = collection.Name;
        existing.Requests = collection.Requests;
        CollectionsChanged?.Invoke(_collections);
    }

    public Task<Collection> ImportFromOpenApiSpec(string specUrl)
        => OpenApiImportResult != null
            ? Task.FromResult(OpenApiImportResult)
            : Task.FromException<Collection>(new InvalidOperationException("No OpenAPI import result configured"));
}
