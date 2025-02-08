using Siren.Components.Http;

namespace Siren.Components
{
    public class SirenAppState
    {
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

        private SirenNetworkRequest? _active;

        public SirenNetworkRequest? Active
        {
            get => _active;
            set
            {
                _active = value;
                ActiveNetworkRequestChanged();
            }
        }

        private List<SirenNetworkRequest>? _networkRequests;

        public List<SirenNetworkRequest> NetworkRequests
        {
            get => _networkRequests ??= new();
            set
            {
                _networkRequests = value;
                NetworkRequestsChanged();
            }
        }

        public event Action? OnChange;

        public event Action? OnNetworkRequestsChanged;

        public event Action<SirenAppState>? OnAppStateChanged;

        public event Action<bool>? OnDarkModeChanged;

        public event Action<int>? OnNetworkRequestIndexChanged;

        public event Action<SirenNetworkRequest?>? OnActiveNetworkRequestChanged;

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

        public void AddNetworkRequest(SirenNetworkRequest request)
        {
            if (!NetworkRequests.Any(x => x.Id.Equals(request.Id)))
            {
                NetworkRequests.Add(request);
                NetworkRequestIndexChanged(NetworkRequests.Count - 1);
            }
            else
            {
                var index = NetworkRequests.FindIndex(x => x.Id.Equals(request.Id));
                NetworkRequests[index] = request;
                NetworkRequestIndexChanged(index);
            }

            SetActive(request);
        }

        public void RemoveNetworkRequest(Guid id)
        {
            bool wasActive = id.Equals(_active?.Id);

            NetworkRequests = NetworkRequests.Where(x => !x.Id.Equals(id)).ToList();

            if (wasActive)
            {
                SetActive(NetworkRequests.LastOrDefault());
            }
        }

        public void SetActive(Guid id)
        {
            var r = NetworkRequests.FirstOrDefault(x => x.Id.Equals(id));

            Active = r;

            ActiveNetworkRequestChanged();
        }

        public void SetActive(SirenNetworkRequest request)
        {
            if (request == null)
            {
                _active = request;
                ActiveNetworkRequestChanged();
                return;
            }

            SetActive(request.Id);
        }

        public void ToggleSideBar()
        {
            SideBarOpen = !SideBarOpen;

            AppStateChanged();
        }
    }
}

