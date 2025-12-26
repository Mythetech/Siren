using System;
using Microsoft.AspNetCore.Components;
using Siren.Components.Services;

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
        string DefaultUserAgent,
        string? DefaultHttpMethod
    );

    public class SettingsState
    {
        private readonly ISettingsService? _settingsService;
        private bool _isInitialized = false;
        private bool _isLoading = false;

        public EventCallback<SettingsState> SettingsCallback;
        public event Action<SettingsState> SettingsChanged;

        public SettingsState(ISettingsService? settingsService = null)
        {
            _settingsService = settingsService;
            LoadSettings();
        }

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

        private string? _defaultHttpMethod = null;
        
        public string? DefaultHttpMethod
        {
            get => _defaultHttpMethod;
            set
            {
                if (_defaultHttpMethod == value) return;
                
                _defaultHttpMethod = value;
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
                DefaultUserAgent,
                DefaultHttpMethod
            );
        }

        /// <summary>
        /// Restores settings from a previously created snapshot and notifies subscribers of changes.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from.</param>
        /// <param name="notify">Whether to notify subscribers and save. Default is true.</param>
        public void RestoreFromSnapshot(SettingsStateSnapshot snapshot, bool notify = true)
        {
            _requestTimeout = snapshot.RequestTimeout;
            _sendRequestsWithSystemToken = snapshot.SendRequestsWithSystemToken;
            _saveHttpContent = snapshot.SaveHttpContent;
            _timeDisplay = snapshot.TimeDisplay;
            _sizeDisplay = snapshot.SizeDisplay;
            _defaultUserAgent = snapshot.DefaultUserAgent;
            _defaultHttpMethod = snapshot.DefaultHttpMethod;
            
            if (notify)
            {
                NotifyChangeSubscribersAsync();
            }
        }

        private async void NotifyChangeSubscribersAsync()
        {
            if (_isInitialized && !_isLoading && _settingsService != null)
            {
                try
                {
                    var snapshot = CreateSnapshot();
                    _settingsService.SaveSettings(snapshot);
                    System.Diagnostics.Debug.WriteLine($"Settings saved: DefaultHttpMethod={snapshot.DefaultHttpMethod}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex}");
                }
            }

            await SettingsCallback.InvokeAsync();
            SettingsChanged?.Invoke(this);
        }

        private void LoadSettings()
        {
            if (_settingsService == null) return;

            try
            {
                _isLoading = true;
                var snapshot = _settingsService.LoadSettings();
                if (snapshot != null)
                {
                    RestoreFromSnapshot(snapshot, notify: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex}");
            }
            finally
            {
                _isLoading = false;
                _isInitialized = true;
            }
        }

    }
}

