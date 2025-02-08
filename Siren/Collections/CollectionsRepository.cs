using LiteDB;
using Siren.Components.Collections;

namespace Siren.Collections
{
    internal static class CollectionsRepository
    {
        private static readonly string DatabaseName = "siren.db";

        private static string GetPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        private static LiteDatabase GetDatabase()
        {
            return new LiteDatabase(Path.Combine(GetPath(), DatabaseName));
        }

        public static void UpsertCollection(Collection collection)
        {
            using var db = GetDatabase();
            var collections = db.GetCollection<PersistentCollection>();

            collections.Upsert(PersistentCollection.Create(collection));
        }

        public static List<Collection> GetCollections()
        {
            using var db = GetDatabase();
            var collections = db.GetCollection<PersistentCollection>();

            return collections.FindAll().Select(x => x.ToCollection()).ToList();
        }

        public static void DeleteCollections()
        {
            using var db = GetDatabase();
            var collections = db.GetCollection<PersistentCollection>();

            collections.DeleteAll();
        }

        public static void DeleteCollection(Guid id)
        {
            using var db = GetDatabase();
            var collections = db.GetCollection<PersistentCollection>();

            collections.Delete(id);
        }
    }
}


