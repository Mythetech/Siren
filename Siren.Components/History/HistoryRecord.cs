using System;
using System.Net;
using Siren.Components.Http;

namespace Siren.Components.History
{
    public class HistoryRecord
    {
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? RequestId { get; set; }

        public string RequestUri { get; set; } = "";

        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

        public HttpStatusCode StatusCode { get; set; }

        public HttpRequest? Request { get; set; }

        public RequestResult? Response { get; set; }

        public string DisplayText { get; set; } = "";
    }
}

