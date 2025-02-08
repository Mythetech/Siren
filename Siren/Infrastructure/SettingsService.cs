using System;
using Siren.Components.Services;

namespace Siren.Infrastructure
{
    public class SettingsService : ISettingsService
    {
        private static readonly string DatabaseName = "siren.db";

        private static string GetPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        private static string GetDatabaseFilePath()
        {
            return Path.Combine(GetPath(), DatabaseName);
        }

        public SettingsService()
        {
        }

        public void PurgeSavedData()
        {
            var dbFilePath = GetDatabaseFilePath();

            if (File.Exists(dbFilePath))
            {
                File.Delete(dbFilePath);
            }
        }

        public long GetAppDataSize()
        {
            var dbFilePath = GetDatabaseFilePath();

            if (File.Exists(dbFilePath))
            {
                var fileInfo = new FileInfo(dbFilePath);
                return fileInfo.Length;
            }

            return 0;
        }
    }
}

