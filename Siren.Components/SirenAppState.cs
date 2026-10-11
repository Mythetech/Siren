using MudBlazor;
using Siren.Components.Http;
using Siren.Components.Http.Models;

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

        // The tabs live in RequestTabsState. The members below only keep components that have not
        // moved to it yet working, and go away once the last of them has moved.
        private readonly RequestTabsState _tabs;

        public SirenAppState(RequestTabsState tabs)
        {
            _tabs = tabs;

            _tabs.TabsChanged += () =>
            {
                NetworkRequestsChanged();
                AppStateChanged();
            };
            _tabs.ActiveTabChanged += _ => ActiveNetworkRequestChanged();
            _tabs.RequestChanged += _ => AppStateChanged();
            _tabs.ResponseChanged += _ => ActiveNetworkRequestChanged();
            _tabs.SendingChanged += _ => AppStateChanged();
        }

        public SirenNetworkRequest? Active
        {
            get => _tabs.Active;
            set
            {
                if (value != null)
                    _tabs.Activate(value.Id);

                ActiveNetworkRequestChanged();
            }
        }

        public List<SirenNetworkRequest> NetworkRequests => _tabs.Tabs.ToList();

        public event Action? OnChange;

        public event Action? OnNetworkRequestsChanged;

        public event Action<SirenAppState>? OnAppStateChanged;

        public event Action<bool>? OnDarkModeChanged;

        public event Action<int>? OnNetworkRequestIndexChanged;

        public event Action<SirenNetworkRequest?>? OnActiveNetworkRequestChanged;

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

        private void NetworkRequestsChanged() => OnNetworkRequestsChanged?.Invoke();

        private void AppStateChanged() => OnAppStateChanged?.Invoke(this);

        private void NetworkRequestIndexChanged(int index) => OnNetworkRequestIndexChanged?.Invoke(index);

        private void ActiveNetworkRequestChanged()
        {
            OnActiveNetworkRequestChanged?.Invoke(Active);
            AppStateChanged();
        }

        private void ActiveEnvironmentChanged()
        {
            OnActiveEnvironmentChanged?.Invoke(ActiveEnvironment);
            AppStateChanged();
        }

        public void AddNetworkRequest(SirenNetworkRequest request)
        {
            _tabs.Open(request);
            NetworkRequestIndexChanged(NetworkRequests.FindIndex(x => x.Id.Equals(request.Id)));
            ActiveNetworkRequestChanged();
        }

        public void RemoveNetworkRequest(Guid id) => _tabs.Close(id);

        public void SetActive(Guid id)
        {
            _tabs.Activate(id);
            ActiveNetworkRequestChanged();
        }

        public void SetActive(SirenNetworkRequest request)
        {
            if (request != null)
                SetActive(request.Id);
        }

        public void ToggleSideBar()
        {
            SideBarOpen = !SideBarOpen;

            AppStateChanged();
        }

        /// <summary>
        /// Call this to notify subscribers when nested properties of Active are modified directly
        /// (e.g., Active.Request.Headers). This ensures all components stay in sync.
        /// </summary>
        public void NotifyActiveRequestChanged()
        {
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

        public event Action<string?, string>? OnImportRequestBody;

        public void ImportRequestBody(string? body, string bodyType)
        {
            OnImportRequestBody?.Invoke(body, bodyType);
        }

        public event Func<Task<(string? body, string bodyType)>>? OnRequestBodyContent;

        public async Task<(string? body, string bodyType)> RequestBodyContentAsync()
        {
            if (OnRequestBodyContent != null)
            {
                return await OnRequestBodyContent.Invoke();
            }
            return (null, "none");
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

