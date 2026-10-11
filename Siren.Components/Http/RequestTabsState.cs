using Siren.Components.Http.Models;
using Siren.Components.Settings;

namespace Siren.Components.Http;

/// <summary>
/// Owns the open request tabs. Components render from it and ask it for changes, and nothing else
/// changes a tab. There is always at least one tab and always an active one.
/// </summary>
public class RequestTabsState
{
    private readonly SettingsState _settings;
    private readonly object _gate = new();

    // Replaced rather than mutated, so a render that is enumerating the tabs is not disturbed
    // when a bus consumer opens or closes one from another thread
    private volatile IReadOnlyList<SirenNetworkRequest> _tabs;
    private volatile SirenNetworkRequest _active;

    public RequestTabsState(SettingsState settings)
    {
        _settings = settings;
        _active = CreateBlankTab();
        _tabs = [_active];
    }

    public IReadOnlyList<SirenNetworkRequest> Tabs => _tabs;

    public SirenNetworkRequest Active => _active;

    public event Action? TabsChanged;

    public event Action<SirenNetworkRequest>? ActiveTabChanged;

    public event Action<SirenNetworkRequest>? RequestChanged;

    public event Action<SirenNetworkRequest>? ResponseChanged;

    public event Action<SirenNetworkRequest>? SendingChanged;

    /// <summary>
    /// Opens a blank request in a new tab and makes it active.
    /// </summary>
    public SirenNetworkRequest OpenNew() => Open(CreateBlankTab());

    /// <summary>
    /// Opens the tab and makes it active. A tab that is already open is only made active.
    /// </summary>
    public SirenNetworkRequest Open(SirenNetworkRequest tab)
    {
        bool added;
        bool activated;

        lock (_gate)
        {
            added = _tabs.All(t => t.Id != tab.Id);
            if (added)
            {
                tab.Request ??= new HttpRequest();
                _tabs = [.. _tabs, tab];
            }
            else
            {
                tab = _tabs.First(t => t.Id == tab.Id);
            }

            activated = !ReferenceEquals(_active, tab);
            _active = tab;
        }

        if (added)
            TabsChanged?.Invoke();

        if (activated)
            ActiveTabChanged?.Invoke(tab);

        return tab;
    }

    /// <summary>
    /// Opens a copy of the tab's request and response in a new tab.
    /// </summary>
    public SirenNetworkRequest? Duplicate(Guid tabId)
    {
        var source = Find(tabId);
        return source == null ? null : Open(source.Copy());
    }

    public void Close(Guid tabId)
    {
        SirenNetworkRequest? activated = null;

        lock (_gate)
        {
            var index = IndexOf(tabId);
            if (index < 0)
                return;

            var remaining = _tabs.Where(t => t.Id != tabId).ToList();
            if (remaining.Count == 0)
                remaining.Add(CreateBlankTab());

            _tabs = remaining;

            if (_active.Id == tabId)
            {
                // The tab that slid into the closed tab's place, which is also what the tab strip picks
                _active = remaining[Math.Min(index, remaining.Count - 1)];
                activated = _active;
            }
        }

        TabsChanged?.Invoke();

        if (activated != null)
            ActiveTabChanged?.Invoke(activated);
    }

    public void Activate(Guid tabId)
    {
        SirenNetworkRequest? tab;

        lock (_gate)
        {
            tab = Find(tabId);
            if (tab == null || ReferenceEquals(tab, _active))
                return;

            _active = tab;
        }

        ActiveTabChanged?.Invoke(tab);
    }

    public void SetUri(Guid tabId, string uri)
        => Change(tabId, request => request.RequestUri == uri, request => request.RequestUri = uri);

    public void SetMethod(Guid tabId, HttpMethod? method)
        => Change(tabId, request => Equals(request.Method, method), request => request.Method = method!);

    public void SetBodyType(Guid tabId, RequestBodyType bodyType)
        => Change(tabId, request => request.BodyType == bodyType, request => request.BodyType = bodyType);

    public void SetRawBody(Guid tabId, string body)
        => Change(tabId, request => request.RawBody == body, request => request.RawBody = body);

    public void SetHeaders(Guid tabId, IEnumerable<KeyValuePair<string, string>> headers)
    {
        var list = headers.ToList();
        Change(tabId, request => (request.Headers ?? []).SequenceEqual(list), request => request.Headers = list);
    }

    public void SetFormData(Guid tabId, IReadOnlyDictionary<string, string> formData)
    {
        var copy = new Dictionary<string, string>(formData);
        Change(tabId, request => SameEntries(request.FormData, copy), request => request.FormData = copy);
    }

    public void SetAttachments(Guid tabId, IEnumerable<BinaryAttachment> attachments)
    {
        var list = attachments.ToList();
        Change(tabId, request => (request.BinaryAttachments ?? []).SequenceEqual(list), request => request.BinaryAttachments = list);
    }

    /// <summary>
    /// Swaps the tab's whole request, for loading a saved request into a tab that is already open.
    /// </summary>
    public void ReplaceRequest(Guid tabId, HttpRequest request)
    {
        var tab = Find(tabId);
        if (tab == null)
            return;

        tab.Request = request;
        RequestChanged?.Invoke(tab);
    }

    internal void SetSending(Guid tabId, bool sending)
    {
        var tab = Find(tabId);
        if (tab == null || tab.IsSending == sending)
            return;

        tab.IsSending = sending;
        SendingChanged?.Invoke(tab);
    }

    /// <summary>
    /// Stores a response on the tab that asked for it. Returns false when that tab has been closed.
    /// </summary>
    internal bool SetResponse(Guid tabId, RequestResult response, string displayText)
    {
        var tab = Find(tabId);
        if (tab == null)
            return false;

        tab.Response = response;
        tab.DisplayText = displayText;
        ResponseChanged?.Invoke(tab);
        return true;
    }

    private void Change(Guid tabId, Func<HttpRequest, bool> isUnchanged, Action<HttpRequest> apply)
    {
        var tab = Find(tabId);
        if (tab?.Request == null || isUnchanged(tab.Request))
            return;

        apply(tab.Request);
        RequestChanged?.Invoke(tab);
    }

    private static bool SameEntries(Dictionary<string, string>? current, Dictionary<string, string> next)
        => current != null
           && current.Count == next.Count
           && next.All(pair => current.TryGetValue(pair.Key, out var value) && value == pair.Value);

    private SirenNetworkRequest? Find(Guid tabId) => _tabs.FirstOrDefault(t => t.Id == tabId);

    private int IndexOf(Guid tabId)
    {
        var tabs = _tabs;
        for (var i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].Id == tabId)
                return i;
        }

        return -1;
    }

    private SirenNetworkRequest CreateBlankTab()
    {
        var request = new HttpRequest();

        if (!string.IsNullOrWhiteSpace(_settings.DefaultHttpMethod))
            request.Method = new HttpMethod(_settings.DefaultHttpMethod);

        return new SirenNetworkRequest { Request = request };
    }
}
