using System;
using LiteDB;
using Siren.Components.Variables;

namespace Siren.Variables
{
    public class VariableRepository
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

        public static void CreateVariable(Variable variable)
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentVariable>();

            collection.Insert(PersistentVariable.Create(variable));
        }

        public static void CreateVariables(List<Variable> variables)
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentVariable>();

            foreach (var variable in variables)
            {
                collection.Insert(PersistentVariable.Create(variable));
            }
        }

        public static void UpdateVariable(Variable variable)
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentVariable>();

            collection.Update(PersistentVariable.Create(variable));
        }

        public static void UpdateVariables(List<Variable> variables)
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentVariable>();

            foreach (var variable in variables)
            {
                collection.Update(PersistentVariable.Create(variable));
            }
        }

        public static void DeleteVariable(Guid id)
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentVariable>();

            collection.Delete(id);
        }

        public static void DeleteVariables(List<Guid> ids)
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentVariable>();

            foreach (var id in ids)
            {
                collection.Delete(id);
            }
        }

        public static List<Variable> GetVariables()
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentVariable>();

            return collection.FindAll().Select(x => x.ToVariable()).ToList();
        }
    }
}

