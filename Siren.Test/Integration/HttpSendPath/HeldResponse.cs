namespace Siren.Test.Integration.HttpSendPath;

/// <summary>
/// A response the test host holds back until the test releases it, so a test can act while a request
/// is in flight without sleeping.
/// </summary>
public sealed class HeldResponse
{
    private readonly TaskCompletionSource _arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrivals;

    public HeldResponse(string url)
    {
        Url = url;
    }

    public string Url { get; }

    /// <summary>
    /// Completes when the request has reached the server.
    /// </summary>
    public Task Arrived => _arrived.Task;

    public int Arrivals => _arrivals;

    public void Release() => _released.TrySetResult();

    internal void MarkArrived()
    {
        Interlocked.Increment(ref _arrivals);
        _arrived.TrySetResult();
    }

    internal Task WaitForReleaseAsync(CancellationToken aborted) => _released.Task.WaitAsync(aborted);
}
