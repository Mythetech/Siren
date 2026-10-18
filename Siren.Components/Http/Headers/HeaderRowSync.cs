using Siren.Components.Http.Models;

namespace Siren.Components.Http.Headers;

/// <summary>
/// Keeps an editor's header rows in step with the headers its tab records. Both header editors use it, so a
/// header changed in one arrives in the other the same way.
/// </summary>
public static class HeaderRowSync
{
    /// <summary>
    /// The headers the rows send: the rows that have a name and are switched on, in order.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, string>> SentHeaders(IEnumerable<HeaderModel> rows) => rows
        .Where(IsSent)
        .Select(row => new KeyValuePair<string, string>(row.Key!, row.Value ?? ""));

    /// <summary>
    /// Whether the rows send what the tab records. A header the tab holds that no row could send, such as one
    /// with no name, does not count.
    /// </summary>
    public static bool InStep(IEnumerable<HeaderModel> rows, IEnumerable<KeyValuePair<string, string>> headers)
        => SentHeaders(rows).SequenceEqual(Sendable(headers));

    /// <summary>
    /// Rows for the tab's headers, made from the rows the editor already has. It is only for headers changed
    /// somewhere else, because an editor's own edits reach the tab in the handler that makes them.
    /// </summary>
    public static List<HeaderModel> Merge(IReadOnlyList<HeaderModel> rows, IReadOnlyList<KeyValuePair<string, string>> headers)
    {
        // Rows are matched to headers of the same name in order, so two rows with one name keep their objects
        var available = rows
            .Where(IsSent)
            .GroupBy(row => row.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => new Queue<HeaderModel>(group), StringComparer.OrdinalIgnoreCase);

        var merged = new List<HeaderModel>(rows.Count + headers.Count);
        foreach (var header in Sendable(headers))
        {
            if (available.TryGetValue(header.Key, out var matches) && matches.TryDequeue(out var row))
            {
                row.Key = header.Key;
                row.Value = header.Value;
                merged.Add(row);
            }
            else
            {
                merged.Add(new HeaderModel { Key = header.Key, Value = header.Value, Enabled = true });
            }
        }

        // A sent row the tab no longer has was renamed or removed elsewhere, so it goes. A blank or switched
        // off row was never on the tab, so it stays where it was.
        for (var index = 0; index < rows.Count; index++)
        {
            if (!IsSent(rows[index]))
                merged.Insert(Math.Min(index, merged.Count), rows[index]);
        }

        return merged;
    }

    // An imported header with no name can never be sent, and would come back as a new row on every merge
    private static IEnumerable<KeyValuePair<string, string>> Sendable(IEnumerable<KeyValuePair<string, string>> headers) => headers
        .Where(header => !string.IsNullOrWhiteSpace(header.Key))
        .Select(header => new KeyValuePair<string, string>(header.Key, header.Value ?? ""));

    private static bool IsSent(HeaderModel row) => !string.IsNullOrWhiteSpace(row.Key) && row.Enabled;
}
