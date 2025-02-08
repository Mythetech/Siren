using System;
namespace Siren.Components.Services
{
    public interface ISettingsService
    {
        public void PurgeSavedData();

        public long GetAppDataSize();
    }
}

