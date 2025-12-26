using System;
using Siren.Components.Settings;

namespace Siren.Components.Services
{
    public interface ISettingsService
    {
        public void PurgeSavedData();

        public long GetAppDataSize();
        
        public SettingsStateSnapshot? LoadSettings();
        
        public void SaveSettings(SettingsStateSnapshot snapshot);
    }
}

