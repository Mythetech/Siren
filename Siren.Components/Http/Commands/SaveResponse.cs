using System.Net;
using Siren.Components.Http.Models;

namespace Siren.Components.Http.Commands
{
    public record SaveResponse(
        HttpStatusCode HttpStatusCode,
        int StatusCode,
        TimeSpan Duration,
        HttpPayloadSize? RequestSize,
        HttpPayloadSize? ResponseSize,
        Dictionary<string, string> Headers,
        Dictionary<string, string> ActualRequestHeaders,
        string ResponseText,
        DateTimeOffset Timestamp
    );
}

