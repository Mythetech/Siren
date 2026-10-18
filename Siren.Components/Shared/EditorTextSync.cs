using Microsoft.Extensions.Logging;

namespace Siren.Components.Shared;

/// <summary>
/// A change an editor reported: the document the text belongs to, and the text.
/// </summary>
public readonly record struct EditorTextChange(Guid DocumentId, string Text);

/// <summary>
/// Keeps an editor and the model that owns its text in step. Every change is read straight away, so
/// the text is on its way to the model before any later click or hotkey is handled. Each read is
/// credited to the document that was loaded when it was issued, and dropped if that document has been
/// loaded again since.
/// </summary>
public sealed class EditorTextSync
{
    private readonly Func<Task<string?>> _read;
    private readonly Func<string, Task> _write;
    private readonly Func<EditorTextChange, Task> _changed;
    private readonly ILogger _logger;

    private readonly Dictionary<Guid, int> _lastLoadOf = new();

    private Task _reads = Task.CompletedTask;
    private Task _readsBeforeLeaving = Task.CompletedTask;
    private bool _reading;
    private bool _changedDuringRead;
    private int _loads;
    private Guid _requestedDocumentId;
    private string _requestedText;

    public EditorTextSync(
        Guid documentId,
        string text,
        Func<Task<string?>> read,
        Func<string, Task> write,
        Func<EditorTextChange, Task> changed,
        ILogger logger)
    {
        DocumentId = documentId;
        LastKnownText = text;
        _requestedDocumentId = documentId;
        _requestedText = text;
        _read = read;
        _write = write;
        _changed = changed;
        _logger = logger;
    }

    public Guid DocumentId { get; private set; }

    public string LastKnownText { get; private set; }

    /// <summary>
    /// Call for every change the editor reports, including the ones its own writes cause.
    /// </summary>
    public Task NotifyChangedAsync()
    {
        if (_reading)
        {
            _changedDuringRead = true;
            return _reads;
        }

        _reads = ReadUntilQuietAsync();
        return _reads;
    }

    /// <summary>
    /// Tells the sync which document and text its owner wants shown. The editor is written to only
    /// when that request changes and no longer matches the editor. Text the editor reported comes
    /// back here as a request a moment later, and an owner that has not caught up yet repeats its
    /// old request. Neither may overwrite what has been typed since.
    /// </summary>
    public Task LoadAsync(Guid documentId, string text)
    {
        if (documentId == _requestedDocumentId && text == _requestedText)
            return Task.CompletedTask;

        _requestedDocumentId = documentId;
        _requestedText = text;

        if (documentId == DocumentId && text == LastKnownText)
            return Task.CompletedTask;

        // A keystroke in the document being left may not have been reported yet. The editor runs calls
        // in the order they are sent, so a read sent before the write still holds that document's text.
        if (documentId != DocumentId)
            ReadBeforeLeaving();

        // The switch and the write are issued together so that every read issued earlier is known
        // to hold the previous load's text, whenever it completes
        _loads++;
        _lastLoadOf[documentId] = _loads;
        DocumentId = documentId;
        LastKnownText = text;
        return _write(text);
    }

    /// <summary>
    /// Completes when every read issued so far has completed and been reported.
    /// </summary>
    public Task FlushAsync() => Task.WhenAll(_reads, _readsBeforeLeaving);

    private void ReadBeforeLeaving()
    {
        var documentId = DocumentId;
        var load = _loads;
        var knownText = LastKnownText;
        var read = ReadAsync();
        _readsBeforeLeaving = ReportReadBeforeLeavingAsync(FlushAsync(), read, documentId, load, knownText);
    }

    private async Task ReportReadBeforeLeavingAsync(Task earlier, Task<string?> read, Guid documentId, int load, string knownText)
    {
        // Reports keep the order the reads were sent in. WhenAny waits for the earlier ones without taking on their failures.
        await Task.WhenAny(earlier);
        var text = await read;

        if (text == null || text == knownText || WasLoadedSince(documentId, load))
            return;

        try
        {
            await _changed(new EditorTextChange(documentId, text));
        }
        catch (Exception ex)
        {
            // The next switch waits on this task, so a failure is logged instead of passed down every later flush
            _logger.LogWarning(ex, "Failed to report the text of the document being left");
        }
    }

    private async Task ReadUntilQuietAsync()
    {
        _reading = true;
        try
        {
            do
            {
                _changedDuringRead = false;

                var documentId = DocumentId;
                var load = _loads;
                var text = await ReadAsync();

                if (text == null || WasLoadedSince(documentId, load))
                    continue;

                if (load == _loads)
                {
                    if (text == LastKnownText)
                        continue;

                    LastKnownText = text;
                }

                await _changed(new EditorTextChange(documentId, text));
            }
            while (_changedDuringRead);
        }
        finally
        {
            _reading = false;
        }
    }

    // A load of the same document replaced its text after the read was sent, so the read holds text that is gone
    private bool WasLoadedSince(Guid documentId, int load)
        => _lastLoadOf.TryGetValue(documentId, out var lastLoad) && lastLoad > load;

    private async Task<string?> ReadAsync()
    {
        try
        {
            return await _read();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read the editor text");
            return null;
        }
    }
}
