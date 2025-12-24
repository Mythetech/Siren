using System;
using System.Net;

namespace Siren.Components.Http.Models
{
    public class RequestResult
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public int StatusCode { get; set; }

        public TimeSpan Duration { get; set; }

        public HttpPayloadSize? RequestSize { get; set; }

        public HttpPayloadSize? ResponseSize { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new();

        public Dictionary<string, string> ActualRequestHeaders { get; set; } = new();

        public List<Cookie>? Cookies { get; set; }

        public HttpContent? ResponseContent { get; set; }

        public Exception? Error { get; set; }

        public string ResponseText { get; set; } = "";

        public NetworkInfo? NetworkInfo { get; set; }

        public RequestTimeline? Timeline { get; set; }

        public RequestResult Copy()
        {
            return new RequestResult
            {
                HttpStatusCode = HttpStatusCode,
                StatusCode = StatusCode,
                Duration = Duration,
                RequestSize = RequestSize,
                ResponseSize = ResponseSize,
                Headers = new Dictionary<string, string>(Headers),
                ActualRequestHeaders = new Dictionary<string, string>(ActualRequestHeaders),
                Cookies = Cookies != null ? [..Cookies] : null,
                ResponseContent = ResponseContent,
                Error = Error,
                ResponseText = ResponseText,
                NetworkInfo = NetworkInfo,
                Timeline = Timeline
            };
        }
    }

    public record HttpPayloadSize(int Body, int Headers);

    public class NetworkInfo
    {
        public string? LocalAddress { get; set; }
        public string? RemoteAddress { get; set; }
        public string? TlsProtocol { get; set; }
        public string? CipherName { get; set; }
        public string? CertificateCommonName { get; set; }
        public string? CertificateIssuer { get; set; }
        public DateTimeOffset? CertificateValidUntil { get; set; }
    }

    public class RequestTimeline
    {
        public TimeSpan? DnsLookup { get; set; }
        public TimeSpan? TcpConnection { get; set; }
        public TimeSpan? TlsHandshake { get; set; }
        public TimeSpan? RequestSent { get; set; }
        public TimeSpan? WaitingForResponse { get; set; }
        public TimeSpan? ContentDownload { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }
}

