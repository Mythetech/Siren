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

        /// <summary>
        /// The fields the rows send. A row without a name is left out, and a later row with the same name
        /// replaces an earlier one, so this never throws.
        /// </summary>
        public static Dictionary<string, string> ToSafeDictionary(this IEnumerable<DictionaryViewModel> viewModels)
        {
            var fields = new Dictionary<string, string>();
            foreach (var row in viewModels.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
            {
                fields[row.Key!] = row.Value ?? "";
            }

            return fields;
        }

        /// <summary>
        /// Whether the rows send exactly what the dictionary says. It is asked while rendering, so it never
        /// throws, and rows that repeat a name match when their last row does.
        /// </summary>
        public static bool HoldSameEntries(this IReadOnlyCollection<DictionaryViewModel> viewModels, IReadOnlyDictionary<string, string> dictionary)
        {
            var sent = viewModels.ToSafeDictionary();

            return sent.Count == dictionary.Count
                && sent.All(field => dictionary.TryGetValue(field.Key, out var value) && value == field.Value);
        }

        /// <summary>
        /// Whether a later row has the same name, which leaves this row out of the request. Names are
        /// compared exactly, as the request's fields are.
        /// </summary>
        public static bool IsShadowed(this IReadOnlyList<DictionaryViewModel> rows, DictionaryViewModel row)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
                return false;

            var after = false;
            foreach (var other in rows)
            {
                if (ReferenceEquals(other, row))
                    after = true;
                else if (after && other.Key == row.Key)
                    return true;
            }

            return false;
        }
    }
}

