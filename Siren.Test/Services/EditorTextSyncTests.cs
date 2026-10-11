using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Siren.Components.Shared;

namespace Siren.Test.Services;

public class EditorTextSyncTests
{
    private static readonly Guid First = Guid.NewGuid();
    private static readonly Guid Second = Guid.NewGuid();

    private readonly List<TaskCompletionSource<string?>> _reads = [];
    private readonly List<string> _writes = [];
    private readonly List<EditorTextChange> _changes = [];
    private int _readsInFlight;
    private int _mostReadsInFlight;

    private EditorTextSync CreateSync(string text = "") => new(
        First,
        text,
        read: () =>
        {
            var read = new TaskCompletionSource<string?>();
            _reads.Add(read);
            _mostReadsInFlight = Math.Max(_mostReadsInFlight, ++_readsInFlight);
            return read.Task;
        },
        write: text =>
        {
            _writes.Add(text);
            return Task.CompletedTask;
        },
        changed: change =>
        {
            _changes.Add(change);
            return Task.CompletedTask;
        },
        NullLogger.Instance);

    private void CompleteRead(int index, string? text)
    {
        _readsInFlight--;
        _reads[index].SetResult(text);
    }

    [Fact(DisplayName = "A change is read and reported once, with the document it belongs to")]
    public async Task Change_IsReportedOnceWithItsDocument()
    {
        var sync = CreateSync();

        var notified = sync.NotifyChangedAsync();
        CompleteRead(0, "typed");
        await notified;

        _changes.ShouldBe([new EditorTextChange(First, "typed")]);
        sync.LastKnownText.ShouldBe("typed");
    }

    [Fact(DisplayName = "Text equal to the last known text is not reported")]
    public async Task UnchangedText_IsNotReported()
    {
        var sync = CreateSync("same");

        var notified = sync.NotifyChangedAsync();
        CompleteRead(0, "same");
        await notified;

        _changes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "The change a load causes is not reported back")]
    public async Task ChangeCausedByLoad_IsNotReported()
    {
        var sync = CreateSync();

        await sync.LoadAsync(Second, "loaded");
        var notified = sync.NotifyChangedAsync();
        CompleteRead(0, "loaded");
        await notified;

        _changes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Changes during a read cause exactly one more read")]
    public async Task ChangesDuringRead_CauseOneMoreRead()
    {
        var sync = CreateSync();

        var first = sync.NotifyChangedAsync();
        var second = sync.NotifyChangedAsync();
        var third = sync.NotifyChangedAsync();
        CompleteRead(0, "a");
        CompleteRead(1, "abc");
        await Task.WhenAll(first, second, third);

        _reads.Count.ShouldBe(2);
        _changes.Select(c => c.Text).ShouldBe(["a", "abc"]);
    }

    [Fact(DisplayName = "Reads never overlap")]
    public async Task Reads_NeverOverlap()
    {
        var sync = CreateSync();

        var first = sync.NotifyChangedAsync();
        var second = sync.NotifyChangedAsync();
        CompleteRead(0, "a");
        var third = sync.NotifyChangedAsync();
        CompleteRead(1, "ab");
        CompleteRead(2, "abc");
        await Task.WhenAll(first, second, third);

        _mostReadsInFlight.ShouldBe(1);
    }

    [Fact(DisplayName = "A read issued before a load is reported against the old document")]
    public async Task ReadIssuedBeforeLoad_IsReportedAgainstOldDocument()
    {
        var sync = CreateSync();

        var notified = sync.NotifyChangedAsync();
        await sync.LoadAsync(Second, "second body");
        CompleteRead(0, "first body, typed");
        await notified;

        _changes.ShouldBe([new EditorTextChange(First, "first body, typed")]);
        sync.DocumentId.ShouldBe(Second);
        sync.LastKnownText.ShouldBe("second body");
    }

    [Fact(DisplayName = "A read issued before the same document is loaded again is still reported")]
    public async Task ReadIssuedBeforeReload_IsReported()
    {
        var sync = CreateSync();

        var notified = sync.NotifyChangedAsync();
        await sync.LoadAsync(Second, "second body");
        await sync.LoadAsync(First, "stale first body");
        CompleteRead(0, "first body, typed");
        await notified;

        _changes.ShouldBe([new EditorTextChange(First, "first body, typed")]);
        sync.LastKnownText.ShouldBe("stale first body");
    }

    [Fact(DisplayName = "A load writes the text to the editor")]
    public async Task Load_WritesText()
    {
        var sync = CreateSync();

        await sync.LoadAsync(Second, "second body");

        _writes.ShouldBe(["second body"]);
        sync.DocumentId.ShouldBe(Second);
    }

    [Fact(DisplayName = "Flush waits for reads that are still out")]
    public async Task Flush_WaitsForOutstandingReads()
    {
        var sync = CreateSync();
        var notified = sync.NotifyChangedAsync();

        var flush = sync.FlushAsync();
        flush.IsCompleted.ShouldBeFalse();
        CompleteRead(0, "typed");
        await Task.WhenAll(notified, flush);

        _changes.ShouldBe([new EditorTextChange(First, "typed")]);
    }

    [Fact(DisplayName = "Flush completes at once when nothing is being read")]
    public void Flush_WithNothingOutstanding_IsComplete()
    {
        CreateSync().FlushAsync().IsCompleted.ShouldBeTrue();
    }

    [Fact(DisplayName = "A read that fails is ignored and later changes are still reported")]
    public async Task FailedRead_IsIgnored()
    {
        var sync = CreateSync();

        var failed = sync.NotifyChangedAsync();
        _readsInFlight--;
        _reads[0].SetException(new InvalidOperationException("editor is gone"));
        await failed;

        var notified = sync.NotifyChangedAsync();
        CompleteRead(1, "typed");
        await notified;

        _changes.ShouldBe([new EditorTextChange(First, "typed")]);
    }

    [Fact(DisplayName = "A read that returns nothing is ignored")]
    public async Task EmptyRead_IsIgnored()
    {
        var sync = CreateSync("body");

        var notified = sync.NotifyChangedAsync();
        CompleteRead(0, null);
        await notified;

        _changes.ShouldBeEmpty();
        sync.LastKnownText.ShouldBe("body");
    }
}
