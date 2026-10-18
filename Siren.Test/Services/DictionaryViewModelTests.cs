using Shouldly;
using Siren.Components.RequestContextPanel;

namespace Siren.Test.Services;

public class DictionaryViewModelTests
{
    private static DictionaryViewModel Row(string? key, string? value = "v") => new() { Key = key, Value = value };

    [Fact(DisplayName = "A later row with the same name replaces an earlier one")]
    public void LaterRow_Wins()
    {
        var rows = new[] { Row("name", "Ada"), Row("name", "Grace") };

        rows.ToSafeDictionary().ShouldBe(new Dictionary<string, string> { ["name"] = "Grace" });
    }

    [Fact(DisplayName = "Rows without a name are left out")]
    public void BlankNames_AreLeftOut()
    {
        var rows = new[] { Row(null), Row(""), Row("  "), Row("role", "admin") };

        rows.ToSafeDictionary().ShouldBe(new Dictionary<string, string> { ["role"] = "admin" });
    }

    [Fact(DisplayName = "Names that differ only in case are two fields")]
    public void CaseDifferentNames_AreTwoFields()
    {
        var rows = new List<DictionaryViewModel> { Row("Key", "1"), Row("key", "2") };

        rows.ToSafeDictionary().Count.ShouldBe(2);
        rows.IsShadowed(rows[0]).ShouldBeFalse();
    }

    [Fact(DisplayName = "Rows that repeat a name hold what their last row sends")]
    public void RepeatedNames_HoldWhatLastRowSends()
    {
        var rows = new List<DictionaryViewModel> { Row("name", "Ada"), Row("name", "Grace") };

        rows.HoldSameEntries(new Dictionary<string, string> { ["name"] = "Grace" }).ShouldBeTrue();
        rows.HoldSameEntries(new Dictionary<string, string> { ["name"] = "Ada" }).ShouldBeFalse();
    }

    [Fact(DisplayName = "Only an earlier row with a repeated name is shadowed")]
    public void OnlyEarlierRepeatedRow_IsShadowed()
    {
        var rows = new List<DictionaryViewModel> { Row("a"), Row("b"), Row("a"), Row(""), Row("") };

        rows.Select(row => rows.IsShadowed(row)).ShouldBe([true, false, false, false, false]);
    }
}
