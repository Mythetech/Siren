using Shouldly;
using Siren.Components.Http.Headers;
using Siren.Components.Http.Models;

namespace Siren.Test.Services;

public class HeaderRowSyncTests
{
    private static HeaderModel Row(string? key, string? value = "v", bool enabled = true)
        => new() { Key = key, Value = value, Enabled = enabled };

    private static KeyValuePair<string, string> Header(string key, string value) => new(key, value);

    [Fact(DisplayName = "Rows the tab still has keep their objects and take its values")]
    public void MatchingRows_KeepObjects()
    {
        var a = Row("X-A", "1");
        var b = Row("X-B", "2");

        var merged = HeaderRowSync.Merge([a, b], [Header("X-A", "9"), Header("X-B", "2")]);

        merged.ShouldBe([a, b]);
        a.Value.ShouldBe("9");
    }

    [Fact(DisplayName = "Two rows with one name are matched in order")]
    public void RepeatedNames_MatchInOrder()
    {
        var first = Row("X", "1");
        var second = Row("X", "2");

        var merged = HeaderRowSync.Merge([first, second], [Header("X", "3"), Header("X", "4")]);

        merged.ShouldBe([first, second]);
        first.Value.ShouldBe("3");
        second.Value.ShouldBe("4");
    }

    [Fact(DisplayName = "A row the tab no longer has is dropped")]
    public void RowGoneFromTab_IsDropped()
    {
        var merged = HeaderRowSync.Merge([Row("X-Old", "1")], [Header("X-New", "1")]);

        merged.Select(row => row.Key).ShouldBe(["X-New"]);
    }

    [Fact(DisplayName = "Blank and switched off rows stay where they were")]
    public void LocalRows_StayInPlace()
    {
        var blank = Row("");
        var off = Row("X-Off", enabled: false);

        var merged = HeaderRowSync.Merge([blank, Row("X-A", "1"), off], [Header("X-A", "1"), Header("X-B", "2")]);

        merged.Select(row => row.Key).ShouldBe(["", "X-A", "X-Off", "X-B"]);
        merged[0].ShouldBeSameAs(blank);
        merged[2].ShouldBeSameAs(off);
    }

    [Fact(DisplayName = "A header renamed elsewhere to a name that is already there keeps both rows")]
    public void RenamedOntoExistingName_KeepsBothRows()
    {
        var a = Row("X-A", "1");

        var merged = HeaderRowSync.Merge([a, Row("X-B", "2")], [Header("X-A", "1"), Header("X-A", "2")]);

        merged.Select(row => row.Key).ShouldBe(["X-A", "X-A"]);
        merged.Select(row => row.Value).ShouldBe(["1", "2"]);
        merged[0].ShouldBeSameAs(a);
    }

    [Fact(DisplayName = "After a merge the rows send the tab's headers in its order")]
    public void Merged_SendsTabHeaders()
    {
        var headers = new[] { Header("X-B", "2"), Header("X-A", "1") };

        var merged = HeaderRowSync.Merge([Row("X-A", "1"), Row(""), Row("X-B", "2")], headers);

        HeaderRowSync.SentHeaders(merged).ShouldBe(headers);
    }
}
