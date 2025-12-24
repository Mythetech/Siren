using System;
using Microsoft.AspNetCore.Components;

namespace Siren.Components.Settings
{
    public enum TimeDisplayOptions
    {
        Milliseconds,
        Seconds
    }

    public enum SizeDisplayOptions
    {
        Bytes,
        Kilobytes,
        Megabytes
    }

    public record SettingsStateSnapshot(
        int RequestTimeout,
        bool SendRequestsWithSystemToken,
        bool SaveHttpContent,
        TimeDisplayOptions TimeDisplay,
        SizeDisplayOptions SizeDisplay,
        string DefaultUserAgent
    );

    public class SettingsState
    {
        public EventCallback<SettingsState> SettingsCallback;
        public event Action<SettingsState> SettingsChanged;

        private int _requestTimeout = 10;
        public int RequestTimeout
        {
            get => _requestTimeout;
            set
            {
                if (_requestTimeout == value) return;
                
                _requestTimeout = value;
                NotifyChangeSubscribersAsync();
            }
        }

        private bool _sendRequestsWithSystemToken = true;
        public bool SendRequestsWithSystemToken
        {
            get => _sendRequestsWithSystemToken;
            set
            {
                if (_sendRequestsWithSystemToken == value) return;
                
                _sendRequestsWithSystemToken = value;
                NotifyChangeSubscribersAsync();
            }
        }

        private bool _saveHttpContent = false;
        
        public bool SaveHttpContent
        {
            get => _saveHttpContent;
            set
            {
                if (_saveHttpContent == value) return;
                
                _saveHttpContent = value;
                NotifyChangeSubscribersAsync();
            }
        }

        private TimeDisplayOptions _timeDisplay = TimeDisplayOptions.Milliseconds;
        
        public TimeDisplayOptions TimeDisplay
        {
            get => _timeDisplay;
            set
            {
                if (_timeDisplay == value) return;
                
                _timeDisplay = value;
                NotifyChangeSubscribersAsync();
            }
        }

        private string _defaultUserAgent = "siren/0.1";
        
        public string DefaultUserAgent
        {
            get => _defaultUserAgent;
            set
            {
                if (_defaultUserAgent == value) return;
                
                _defaultUserAgent = value;
                NotifyChangeSubscribersAsync();
            }
        }

        private SizeDisplayOptions _sizeDisplay = SizeDisplayOptions.Bytes;
        
        public SizeDisplayOptions SizeDisplay
        {
            get => _sizeDisplay;
            set
            {
                if (_sizeDisplay == value) return;
                
                _sizeDisplay = value;
                NotifyChangeSubscribersAsync();
            }
        }

        /// <summary>
        /// Creates a snapshot of the current settings state for later restoration.
        /// </summary>
        /// <returns>A snapshot containing all current setting values.</returns>
        public SettingsStateSnapshot CreateSnapshot()
        {
            return new SettingsStateSnapshot(
                RequestTimeout,
                SendRequestsWithSystemToken,
                SaveHttpContent,
                TimeDisplay,
                SizeDisplay,
                DefaultUserAgent
            );
        }

        /// <summary>
        /// Restores settings from a previously created snapshot and notifies subscribers of changes.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from.</param>
        public void RestoreFromSnapshot(SettingsStateSnapshot snapshot)
        {
            _requestTimeout = snapshot.RequestTimeout;
            _sendRequestsWithSystemToken = snapshot.SendRequestsWithSystemToken;
            _saveHttpContent = snapshot.SaveHttpContent;
            _timeDisplay = snapshot.TimeDisplay;
            _sizeDisplay = snapshot.SizeDisplay;
            _defaultUserAgent = snapshot.DefaultUserAgent;
            NotifyChangeSubscribersAsync();
        }

        private async void NotifyChangeSubscribersAsync()
        {
            await SettingsCallback.InvokeAsync();
            SettingsChanged?.Invoke(this);
        }

    }
}

