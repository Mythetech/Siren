using System.Net.Http.Headers;
using System.Text;

namespace Siren.Components.Http;

public static class RawBodyContent
{
    private const string DefaultMediaType = "application/json";
    private const string ContentTypeHeader = "Content-Type";

    /// <summary>
    /// Builds the content for a raw request body. A content type the framework would reject, such as one
    /// that carries parameters, is sent exactly as typed instead of failing the request.
    /// </summary>
    public static StringContent Create(string payload, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return new StringContent(payload, Encoding.UTF8, DefaultMediaType);

        if (MediaTypeHeaderValue.TryParse(contentType, out var parsed) && parsed.Parameters.Count == 0 && parsed.MediaType != null)
            return new StringContent(payload, Encoding.UTF8, parsed.MediaType);

        var content = new StringContent(payload, Encoding.UTF8);
        content.Headers.Remove(ContentTypeHeader);
        content.Headers.TryAddWithoutValidation(ContentTypeHeader, contentType);

        return content;
    }
}
