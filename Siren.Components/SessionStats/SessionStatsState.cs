namespace Siren.Components.SessionStats;

/// <summary>
/// Tracks session-level statistics for HTTP requests.
/// Resets when the application restarts.
/// </summary>
public class SessionStatsState
{
    private readonly object _lock = new();

    /// <summary>
    /// Current streak of consecutive successful (2xx/3xx) requests.
    /// </summary>
    public int SuccessStreak { get; private set; }

    /// <summary>
    /// Fastest response time in milliseconds this session.
    /// </summary>
    public double? FastestResponseMs { get; private set; }

    /// <summary>
    /// The URL that achieved the fastest response time.
    /// </summary>
    public string? FastestResponseUrl { get; private set; }

    /// <summary>
    /// Hit count per domain (host only, ignoring path and query).
    /// </summary>
    public Dictionary<string, int> DomainHits { get; } = new();

    /// <summary>
    /// Total number of requests made this session.
    /// </summary>
    public int TotalRequests { get; private set; }

    /// <summary>
    /// Event raised when stats change.
    /// </summary>
    public event Action? OnStatsChanged;

    /// <summary>
    /// Records a completed request and updates stats.
    /// </summary>
    /// <param name="url">The request URL</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    public void RecordRequest(string url, int statusCode, double responseTimeMs)
    {
        lock (_lock)
        {
            TotalRequests++;

            // Update streak - 2xx and 3xx are considered successful
            if (statusCode >= 200 && statusCode < 400)
            {
                SuccessStreak++;
            }
            else
            {
                SuccessStreak = 0;
            }

            // Update fastest response time
            if (!FastestResponseMs.HasValue || responseTimeMs < FastestResponseMs.Value)
            {
                FastestResponseMs = responseTimeMs;
                FastestResponseUrl = url;
            }

            // Update domain hits
            var domain = ExtractDomain(url);
            if (!string.IsNullOrEmpty(domain))
            {
                DomainHits.TryGetValue(domain, out var count);
                DomainHits[domain] = count + 1;
            }
        }

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Gets the most frequently hit domain and its count.
    /// </summary>
    public (string? Domain, int Count) GetTopDomain()
    {
        lock (_lock)
        {
            if (DomainHits.Count == 0)
                return (null, 0);

            var top = DomainHits.MaxBy(kvp => kvp.Value);
            return (top.Key, top.Value);
        }
    }

    /// <summary>
    /// Resets all session statistics.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            SuccessStreak = 0;
            FastestResponseMs = null;
            FastestResponseUrl = null;
            DomainHits.Clear();
            TotalRequests = 0;
        }

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Extracts the domain (host) from a URL.
    /// </summary>
    private static string? ExtractDomain(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return null;
        }
    }
}
