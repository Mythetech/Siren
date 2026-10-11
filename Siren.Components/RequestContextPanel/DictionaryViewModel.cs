using System;
namespace Siren.Components.RequestContextPanel
{
    public class DictionaryViewModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? Key { get; set; }

        public string? Value { get; set; }

        public bool IsSystemHeader { get; set; }
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

        /// <summary>
        /// Whether the rows that have a name say exactly what the dictionary says. It is asked while
        /// rendering, so it never throws: two rows with the same name simply do not match.
        /// </summary>
        public static bool HoldSameEntries(this IReadOnlyCollection<DictionaryViewModel> viewModels, IReadOnlyDictionary<string, string> dictionary)
        {
            var named = viewModels.Where(x => !string.IsNullOrWhiteSpace(x.Key)).ToList();

            return named.Count == dictionary.Count
                && named.Select(x => x.Key).Distinct().Count() == named.Count
                && named.All(x => dictionary.TryGetValue(x.Key!, out var value) && value == (x.Value ?? ""));
        }
    }
}

