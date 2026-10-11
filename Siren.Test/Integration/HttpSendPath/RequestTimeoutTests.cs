using Shouldly;
using Siren.Components.Http.Models;
using Siren.Test.Infrastructure;

namespace Siren.Test.Integration.HttpSendPath;

public class RequestTimeoutTests : IClassFixture<EchoHostFixture>
{
    // A request that is still running after this long counts as hung, and the test fails
    private static readonly TimeSpan TestLimit = TimeSpan.FromSeconds(5);

    private readonly EchoHostFixture _host;

    public RequestTimeoutTests(EchoHostFixture host)
    {
        _host = host;
    }

    private static HttpRequest Get(string url, TimeSpan? timeout = null)
    {
        var request = new HttpRequest { Method = HttpMethod.Get, RequestUri = url, Content = null! };

        if (timeout != null)
            request.Timeout = timeout.Value;

        return request;
    }

    [Fact(DisplayName = "A request that exceeds its timeout ends with a timeout error")]
    public async Task Timeout_BeforeHeaders_ReturnsTimeoutError()
    {
        var held = _host.Hold();
        try
        {
            var result = await TestHttpRequestClient.Create()
                .SendHttpRequestAsync(Get(held.Url, TimeSpan.FromMilliseconds(200)), CancellationToken.None)
                .WaitAsync(TestLimit);

            result.Error.ShouldBeOfType<TimeoutException>();
        }
        finally
        {
            held.Release();
        }
    }

    [Fact(DisplayName = "The timeout also applies while the body downloads")]
    public async Task Timeout_DuringBody_ReturnsTimeoutError()
    {
        var held = _host.Hold("/slow-body");
        try
        {
            var result = await TestHttpRequestClient.Create()
                .SendHttpRequestAsync(Get(held.Url, TimeSpan.FromMilliseconds(300)), CancellationToken.None)
                .WaitAsync(TestLimit);

            result.Error.ShouldBeOfType<TimeoutException>();
        }
        finally
        {
            held.Release();
        }
    }

    [Fact(DisplayName = "Cancelling while the body downloads returns an error result")]
    public async Task Cancel_DuringBody_ReturnsErrorResult()
    {
        var held = _host.Hold("/slow-body");
        using var cancellation = new CancellationTokenSource();
        try
        {
            var send = TestHttpRequestClient.Create().SendHttpRequestAsync(Get(held.Url), cancellation.Token);
            await held.Arrived.WaitAsync(TestLimit);
            await cancellation.CancelAsync();

            var result = await send.WaitAsync(TestLimit);

            result.Error.ShouldBeAssignableTo<OperationCanceledException>();
        }
        finally
        {
            held.Release();
        }
    }

    [Fact(DisplayName = "Cancelling during a retry delay returns an error result instead of throwing")]
    public async Task Cancel_DuringRetryDelay_ReturnsErrorResult()
    {
        var dropped = _host.Hold("/abort");
        var settings = TestState.Settings(http =>
        {
            http.RetryAttempts = 1;
            http.RetryDelayMs = 30_000;
        });
        using var cancellation = new CancellationTokenSource();

        var send = TestHttpRequestClient.Create(settings).SendHttpRequestAsync(Get(dropped.Url), cancellation.Token);
        await dropped.Arrived.WaitAsync(TestLimit);
        // The client needs a moment to see the dropped connection and start waiting for its retry
        await Task.Delay(200);
        await cancellation.CancelAsync();

        var result = await send.WaitAsync(TestLimit);

        result.Error.ShouldNotBeNull();
        dropped.Arrivals.ShouldBe(1);
    }

    [Fact(DisplayName = "Cancelling before the response arrives is reported as cancelled, not as a timeout")]
    public async Task Cancel_BeforeHeaders_IsNotATimeout()
    {
        var held = _host.Hold();
        using var cancellation = new CancellationTokenSource();
        try
        {
            var send = TestHttpRequestClient.Create().SendHttpRequestAsync(Get(held.Url), cancellation.Token);
            await held.Arrived.WaitAsync(TestLimit);
            await cancellation.CancelAsync();

            var result = await send.WaitAsync(TestLimit);

            result.Error.ShouldBeAssignableTo<OperationCanceledException>();
        }
        finally
        {
            held.Release();
        }
    }

    [Fact(DisplayName = "The timeout also applies while the host name is looked up")]
    public async Task Timeout_DuringHostLookup_ReturnsTimeoutError()
    {
        var result = await new HangingLookupClient()
            .SendHttpRequestAsync(Get($"{_host.BaseUrl}/echo", TimeSpan.FromMilliseconds(200)), CancellationToken.None)
            .WaitAsync(TestLimit);

        result.Error.ShouldBeOfType<TimeoutException>();
    }

    [Fact(DisplayName = "Cancelling while the host name is looked up returns an error result")]
    public async Task Cancel_DuringHostLookup_ReturnsErrorResult()
    {
        var client = new HangingLookupClient();
        using var cancellation = new CancellationTokenSource();

        var send = client.SendHttpRequestAsync(Get($"{_host.BaseUrl}/echo"), cancellation.Token);
        await client.LookupStarted.WaitAsync(TestLimit);
        await cancellation.CancelAsync();

        var result = await send.WaitAsync(TestLimit);

        result.Error.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Theory(DisplayName = "A zero, negative or oversized timeout means no limit")]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(int.MaxValue)]
    public async Task Timeout_OutOfRange_MeansNoLimit(int seconds)
    {
        var result = await TestHttpRequestClient.Create()
            .SendHttpRequestAsync(Get($"{_host.BaseUrl}/echo", TimeSpan.FromSeconds(seconds)), CancellationToken.None)
            .WaitAsync(TestLimit);

        result.Error.ShouldBeNull();
        result.StatusCode.ShouldBe(200);
    }
}
