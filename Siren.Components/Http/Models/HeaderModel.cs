using System;
namespace Siren.Components.Http.Models
{
    public class HeaderModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? Key { get; set; }

        public string? Value { get; set; }

        public bool IsSystemHeader { get; set; }

        public bool Enabled { get; set; } = true;
    }

    public static class HeaderModelExtensions
    {
        public static List<HeaderModel> ToViewModel(this Dictionary<string, string> dictionary)
        {
            return dictionary.Select(x => new HeaderModel()
            {
                Key = x.Key,
                Value = x.Value,
                Enabled = true,
            }).ToList();
        }

        public static List<HeaderModel> ToViewModel(this List<KeyValuePair<string, string>> headers)
        {
            return headers.Select(x => new HeaderModel()
            {
                Key = x.Key,
                Value = x.Value,
                Enabled = true,
            }).ToList();
        }
    }
}

