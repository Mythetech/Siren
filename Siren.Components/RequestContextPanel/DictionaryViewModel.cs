using System;
namespace Siren.Components.RequestContextPanel
{
    public class DictionaryViewModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? Key { get; set; }

        public string? Value { get; set; }
    }

    public static class DictionaryViewModelExtensions
    {
        public static List<DictionaryViewModel> ToViewModel(this Dictionary<string, string> dictionary)
        {
            return dictionary.Select(x => new DictionaryViewModel()
            {
                Key = x.Key,
                Value = x.Value,
            }).ToList();
        }

        public static Dictionary<string, string> ToSafeDictionary(this IEnumerable<DictionaryViewModel> viewModels)
        {
            return viewModels
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key, x => x.Value ?? "");
        }
    }
}

