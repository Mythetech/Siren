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
    private readonly List<string> _calls = [];
    private bool _failNextChange;
    private int _readsInFlight;
    private int _mostReadsInFlight;

    private EditorTextSync CreateSync(string text = "") => new(
        First,
        text,
        read: () =>
        {
            _calls.Add("read");
            var read = new TaskCompletionSource<string?>();
            _reads.Add(read);
            _mostReadsInFlight = Math.Max(_mostReadsInFlight, ++_readsInFlight);
            return read.Task;
        },
        write: text =>
        {
            _calls.Add($"write {text}");
            _writes.Add(text);
            return Task.CompletedTask;
        },
        changed: change =>
        {
            if (_failNextChange)
            {
                _failNextChange = false;
                throw new InvalidOperationException("model broke");
            }

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
        CompleteRead(0, "");
        var notified = sync.NotifyChangedAsync();
        CompleteRead(1, "loaded");
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

    [Fact(DisplayName = "A read sent before its document is loaded again is dropped")]
    public async Task ReadIssuedBeforeReload_IsDropped()
    {
        var sync = CreateSync();

        var notified = sync.NotifyChangedAsync();
        await sync.LoadAsync(Second, "second body");
        await sync.LoadAsync(First, "stale first body");
        CompleteRead(0, "first body, typed");
        await notified;

        _changes.ShouldBeEmpty();
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

    [Fact(DisplayName = "An owner that repeats its old request does not overwrite what was typed")]
    public async Task RepeatedRequest_DoesNotOverwriteTypedText()
    {
        var sync = CreateSync();
        var notified = sync.NotifyChangedAsync();
        CompleteRead(0, "typed");
        await notified;

        await sync.LoadAsync(First, "");

        _writes.ShouldBeEmpty();
        sync.LastKnownText.ShouldBe("typed");
    }

    [Fact(DisplayName = "Text the editor reported is not written back when the owner asks for it")]
    public async Task ReportedTextRequestedBack_IsNotWritten()
    {
        var sync = CreateSync();
        var notified = sync.NotifyChangedAsync();
        CompleteRead(0, "typed");
        await notified;

        await sync.LoadAsync(First, "typed");

        _writes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "A request for other text than the editor shows is written")]
    public async Task ChangedRequest_IsWritten()
    {
        var sync = CreateSync();
        var notified = sync.NotifyChangedAsync();
        CompleteRead(0, "typed");
        await notified;

        await sync.LoadAsync(First, "loaded from elsewhere");

        _writes.ShouldBe(["loaded from elsewhere"]);
        sync.LastKnownText.ShouldBe("loaded from elsewhere");
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

    [Fact(DisplayName = "A switch reads the document being left before writing the next one")]
    public async Task Switch_ReadsLeavingDocumentFirst()
    {
        var sync = CreateSync("first body");

        await sync.LoadAsync(Second, "second body");
        CompleteRead(0, "first body, typed");
        await sync.FlushAsync();

        _calls.ShouldBe(["read", "write second body"]);
        _changes.ShouldBe([new EditorTextChange(First, "first body, typed")]);
    }

    [Fact(DisplayName = "The read before a switch reports nothing when the text being left did not change")]
    public async Task Switch_UnchangedText_ReportsNothing()
    {
        var sync = CreateSync("first body");

        await sync.LoadAsync(Second, "second body");
        CompleteRead(0, "first body");
        await sync.FlushAsync();

        _changes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "The read before a switch is reported after the reads sent before it")]
    public async Task Switch_ReadIsReportedInOrder()
    {
        var sync = CreateSync();

        var notified = sync.NotifyChangedAsync();
        await sync.LoadAsync(Second, "second body");
        CompleteRead(1, "first body, typed twice");
        CompleteRead(0, "first body, typed");
        await notified;
        await sync.FlushAsync();

        _changes.Select(change => change.Text).ShouldBe(["first body, typed", "first body, typed twice"]);
    }

    [Fact(DisplayName = "A read sent before the document's request is replaced is dropped")]
    public async Task ReadIssuedBeforeReplace_IsDropped()
    {
        var sync = CreateSync("old body");

        var notified = sync.NotifyChangedAsync();
        await sync.LoadAsync(First, "replaced body");
        CompleteRead(0, "old body, typed");
        await notified;

        _changes.ShouldBeEmpty();
        sync.LastKnownText.ShouldBe("replaced body");
        _writes.ShouldBe(["replaced body"]);
    }

    [Fact(DisplayName = "A report before a switch that fails does not stop later switches from reporting")]
    public async Task FailedReportBeforeSwitch_DoesNotStopLaterReports()
    {
        var sync = CreateSync("first body");
        _failNextChange = true;
        await sync.LoadAsync(Second, "second body");
        CompleteRead(0, "first body, typed");

        await sync.LoadAsync(First, "first body");
        CompleteRead(1, "second body, typed");
        await sync.FlushAsync();

        _changes.ShouldBe([new EditorTextChange(Second, "second body, typed")]);
    }
}
