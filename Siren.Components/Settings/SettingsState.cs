using System;
using Microsoft.AspNetCore.Components;

namespace Siren.Components.Settings
{
    public enum TimeDisplayOptions
    {
        Milliseconds,
        Seconds
    }

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

        private async void NotifyChangeSubscribersAsync()
        {
            await SettingsCallback.InvokeAsync();
            SettingsChanged?.Invoke(this);
        }

    }
}

