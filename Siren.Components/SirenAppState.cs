using MudBlazor;

namespace Siren.Components
{
    public class SirenAppState
    {
        private MudThemeProvider? _themeProvider = default;
        
        public void SetThemeProvider(MudThemeProvider provider)
            => _themeProvider = provider;
        
        private bool _sidebarOpen = false;

        public bool SideBarOpen
        {
            get => _sidebarOpen;
            set
            {
                _sidebarOpen = value;
                AppBarChanged();
                AppStateChanged();
            }
        }

        private bool _isDarkMode = false;

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                IsDarkModeChanged();
                AppStateChanged();
            }
        }

        public event Action? OnChange;

        public event Action<SirenAppState>? OnAppStateChanged;

        public event Action<bool>? OnDarkModeChanged;

        public event Action<string?>? OnActiveEnvironmentChanged;

        private string? _activeEnvironment;

        public string? ActiveEnvironment
        {
            get => _activeEnvironment;
            set
            {
                _activeEnvironment = value;
                ActiveEnvironmentChanged();
            }
        }

        private void AppBarChanged() => OnChange?.Invoke();

        private void IsDarkModeChanged() => OnDarkModeChanged?.Invoke(IsDarkMode);

        private void AppStateChanged() => OnAppStateChanged?.Invoke(this);

        private void ActiveEnvironmentChanged()
        {
            OnActiveEnvironmentChanged?.Invoke(ActiveEnvironment);
            AppStateChanged();
        }

        public void ToggleSideBar()
        {
            SideBarOpen = !SideBarOpen;

            AppStateChanged();
        }

        public async Task SetSystemColorMode()
        {
            if (_themeProvider == null)
                return;

            IsDarkMode = await _themeProvider.GetSystemDarkModeAsync();
        }

        public event Func<Task>? OnFormatRequest;

        public async Task TriggerFormatRequest()
        {
            if (OnFormatRequest != null)
                await OnFormatRequest.Invoke();
        }

        public event Func<string, Task>? OnOpenContextPanel;

        public async Task TriggerOpenContextPanel(string panelName)
        {
            if (OnOpenContextPanel != null)
                await OnOpenContextPanel.Invoke(panelName);
        }

        public event Func<string, Task>? OnOpenRequestContextPanel;

        public async Task TriggerOpenRequestContextPanel(string tabName)
        {
            if (OnOpenRequestContextPanel != null)
                await OnOpenRequestContextPanel.Invoke(tabName);
        }

        public event Func<Task>? OnOpenSettings;

        public async Task TriggerOpenSettings()
        {
            if (OnOpenSettings != null)
                await OnOpenSettings.Invoke();
        }
    }
}
