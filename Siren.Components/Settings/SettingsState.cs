using System;
using Microsoft.AspNetCore.Components;

namespace Siren.Components.Settings
{
    public class SettingsState
    {
        public EventCallback<SettingsState> SettingsChanged;

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

        private async void NotifyChangeSubscribersAsync()
        {
            await SettingsChanged.InvokeAsync();
        }

    }
}

