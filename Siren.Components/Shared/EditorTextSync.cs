using Microsoft.Extensions.Logging;

namespace Siren.Components.Shared;

/// <summary>
/// A change an editor reported: the document the text belongs to, and the text.
/// </summary>
public readonly record struct EditorTextChange(Guid DocumentId, string Text);

/// <summary>
/// Keeps an editor and the model that owns its text in step. Every change is read straight away, so
/// the text is on its way to the model before any later click or hotkey is handled, and each read is
/// credited to the document that was loaded when the read was issued.
/// </summary>
public sealed class EditorTextSync
{
    private readonly Func<Task<string?>> _read;
    private readonly Func<string, Task> _write;
    private readonly Func<EditorTextChange, Task> _changed;
    private readonly ILogger _logger;

    private Task _reads = Task.CompletedTask;
    private bool _reading;
    private bool _changedDuringRead;
    private int _loads;

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
    /// Shows another document, or new text for the same one.
    /// </summary>
    public Task LoadAsync(Guid documentId, string text)
    {
        // The switch and the write are issued together so that every read issued earlier is known
        // to hold the previous load's text, whenever it completes
        _loads++;
        DocumentId = documentId;
        LastKnownText = text;
        return _write(text);
    }

    /// <summary>
    /// Completes when every read issued so far has completed and been reported.
    /// </summary>
    public Task FlushAsync() => _reads;

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

                if (text == null)
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
