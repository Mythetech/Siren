namespace Siren.Components.Http.Models;

public class QueryParameterModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Key { get; set; }
    public string? Value { get; set; }
    public bool Enabled { get; set; } = true;
}

public static class QueryParameterModelExtensions
{
    public static List<QueryParameterModel> ToQueryParamViewModels(this Dictionary<string, string>? dictionary)
    {
        if (dictionary == null) return new();
        return dictionary.Select(x => new QueryParameterModel
        {
            Key = x.Key,
            Value = x.Value,
            Enabled = true,
        }).ToList();
    }

    public static Dictionary<string, string> ToSafeDictionary(this IEnumerable<QueryParameterModel> viewModels)
    {
        return viewModels
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Enabled)
            .ToDictionary(x => x.Key!, x => x.Value ?? "");
    }
}
