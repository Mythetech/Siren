using LiteDB;
using Siren.Components.Settings;

namespace Siren.Infrastructure
{
    public class PersistentSettings
    {
        [BsonId]
        public int DocumentId { get; set; }
        
        public int RequestTimeout { get; set; }
        
        public bool SendRequestsWithSystemToken { get; set; }
        
        public bool SaveHttpContent { get; set; }
        
        public TimeDisplayOptions TimeDisplay { get; set; }
        
        public SizeDisplayOptions SizeDisplay { get; set; }
        
        public string DefaultUserAgent { get; set; } = "";
        
        public string? DefaultHttpMethod { get; set; }

        public static PersistentSettings Create(SettingsStateSnapshot snapshot)
        {
            return new PersistentSettings
            {
                RequestTimeout = snapshot.RequestTimeout,
                SendRequestsWithSystemToken = snapshot.SendRequestsWithSystemToken,
                SaveHttpContent = snapshot.SaveHttpContent,
                TimeDisplay = snapshot.TimeDisplay,
                SizeDisplay = snapshot.SizeDisplay,
                DefaultUserAgent = snapshot.DefaultUserAgent,
                DefaultHttpMethod = snapshot.DefaultHttpMethod
            };
        }

        public SettingsStateSnapshot ToSnapshot()
        {
            return new SettingsStateSnapshot(
                RequestTimeout,
                SendRequestsWithSystemToken,
                SaveHttpContent,
                TimeDisplay,
                SizeDisplay,
                DefaultUserAgent,
                DefaultHttpMethod
            );
        }
    }
}

