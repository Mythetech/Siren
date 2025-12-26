using System;
using LiteDB;
using Siren.Components.Settings;

namespace Siren.Infrastructure
{
    public class SettingsRepository
    {
        private static readonly string DatabaseName = "siren.db";
        private const int SettingsDocumentId = 1;

        private static string GetPath()
        {
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        }

        private static LiteDatabase GetDatabase()
        {
            return new LiteDatabase(Path.Combine(GetPath(), DatabaseName));
        }

        public static void SaveSettings(SettingsStateSnapshot snapshot)
        {
            try
            {
                using var db = GetDatabase();
                var collection = db.GetCollection<PersistentSettings>();

                var persistent = PersistentSettings.Create(snapshot);
                persistent.DocumentId = SettingsDocumentId;
                
                var result = collection.Upsert(persistent);
                System.Diagnostics.Debug.WriteLine($"Settings saved: Upsert result = {result}, DefaultHttpMethod = {snapshot.DefaultHttpMethod}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex}");
                throw;
            }
        }

        public static SettingsStateSnapshot? LoadSettings()
        {
            try
            {
                using var db = GetDatabase();
                var collection = db.GetCollection<PersistentSettings>();

                var persistent = collection.FindById(SettingsDocumentId);
                
                if (persistent == null)
                {
                    System.Diagnostics.Debug.WriteLine("No settings found in database");
                    return null;
                }

                var snapshot = persistent.ToSnapshot();
                System.Diagnostics.Debug.WriteLine($"Settings loaded: DefaultHttpMethod = {snapshot.DefaultHttpMethod}");
                return snapshot;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex}");
                return null;
            }
        }
    }
}

