using LiteDB;

namespace Siren.Components.Http.Models
{
    public class HttpRequest
    {
        public HttpMethod Method { get; set; } = default!;

        public string RequestUri { get; set; } = "";

        public string DisplayUri { get; set; } = "";

        public Dictionary<string, string>? QueryParameters { get; set; }

        public List<KeyValuePair<string, string>> Headers { get; set; } = new();

        public Dictionary<string, string> FormData { get; set; } = new();

        public List<BinaryAttachment> BinaryAttachments { get; set; } = new();
        
        [BsonIgnore]
        public HttpContent Content { get; set; }

        public string ContentType { get; set; } = "application/json";

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);

        public int RetryAttempts { get; set; } = 3;

        public Guid Id { get; set; } = Guid.NewGuid();

        public RequestBodyType BodyType { get; set; } = RequestBodyType.Raw;

        public string RawBody { get; set; } = "";

        public HttpRequest Copy()
        {
            return new HttpRequest
            {
                Method = new HttpMethod(Method.ToString()),
                RequestUri = RequestUri,
                DisplayUri = DisplayUri,
                QueryParameters = QueryParameters?.ToDictionary(entry => entry.Key, entry => entry.Value),
                Headers = new List<KeyValuePair<string, string>>(Headers),
                FormData = new Dictionary<string, string>(FormData),
                BinaryAttachments = BinaryAttachments?.Select(a => a.Copy()).ToList() ?? new List<BinaryAttachment>(),
                Content = Content, 
                ContentType = ContentType,
                Timeout = Timeout,
                RetryAttempts = RetryAttempts,
                Id = Guid.NewGuid(),
                BodyType = BodyType,
                RawBody = RawBody
            };
        }
    }
}

