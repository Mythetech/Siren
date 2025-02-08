﻿using LiteDB;
using Siren.Components.History;

namespace Siren.History
{
    internal static class HistoryRepository
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

        public static void UpsertHistoryRecord(HistoryRecord record)
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentHistoryRecord>();

            collection.Insert(PersistentHistoryRecord.Create(record));
        }

        public static List<HistoryRecord> GetHistoryRecords()
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentHistoryRecord>();

            var records = new List<HistoryRecord>();

            try
            {
                records = collection.FindAll().Select(x => x.ToHistoryRecord()).ToList();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return records;
        }

        public static void DeleteAllHistoryRecords()
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<PersistentHistoryRecord>();

            collection.DeleteAll();
        }
    }
}

