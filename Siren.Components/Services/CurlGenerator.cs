using System.Text;
using Siren.Components.Http.Models;

namespace Siren.Components.Services;

/// <summary>
/// Service for generating cURL commands from HTTP requests.
/// </summary>
public interface ICurlGenerator
{
    string GenerateCurl(HttpRequest request);
}

public class CurlGenerator : ICurlGenerator
{
    /// <summary>
    /// Generates a cURL command string from an HttpRequest object.
    /// </summary>
    /// <param name="request">The HTTP request to convert.</param>
    /// <returns>A cURL command string.</returns>
    public string GenerateCurl(HttpRequest request)
    {
        if (request == null)
            return "";

        var sb = new StringBuilder();
        sb.Append("curl");

        // Add method (skip for GET as it's the default)
        if (request.Method != HttpMethod.Get)
        {
            sb.Append($" -X {request.Method.Method}");
        }

        // Add URL
        var url = request.RequestUri;
        if (!string.IsNullOrEmpty(url))
        {
            sb.Append($" '{EscapeSingleQuotes(url)}'");
        }

        // Add headers
        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
            {
                sb.Append($" \\\n  -H '{EscapeSingleQuotes(header.Key)}: {EscapeSingleQuotes(header.Value)}'");
            }
        }

        // Add body based on body type
        switch (request.BodyType)
        {
            case RequestBodyType.Raw:
                if (!string.IsNullOrEmpty(request.RawBody))
                {
                    sb.Append($" \\\n  --data-raw '{EscapeSingleQuotes(request.RawBody)}'");
                }
                break;

            case RequestBodyType.FormData:
                if (request.FormData != null && request.FormData.Count > 0)
                {
                    var formDataParts = request.FormData
                        .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
                    var formDataString = string.Join("&", formDataParts);
                    sb.Append($" \\\n  -d '{EscapeSingleQuotes(formDataString)}'");
                }
                break;

            case RequestBodyType.MultipartFormData:
                if (request.FormData != null)
                {
                    foreach (var kvp in request.FormData)
                    {
                        sb.Append($" \\\n  -F '{EscapeSingleQuotes(kvp.Key)}={EscapeSingleQuotes(kvp.Value)}'");
                    }
                }
                if (request.BinaryAttachments != null)
                {
                    foreach (var attachment in request.BinaryAttachments)
                    {
                        sb.Append($" \\\n  -F 'file=@{EscapeSingleQuotes(attachment.FileName)}'");
                    }
                }
                break;

            case RequestBodyType.Binary:
                if (request.BinaryAttachments != null && request.BinaryAttachments.Count > 0)
                {
                    var attachment = request.BinaryAttachments[0];
                    sb.Append($" \\\n  --data-binary '@{EscapeSingleQuotes(attachment.FileName)}'");
                }
                break;
        }

        return sb.ToString();
    }

    private static string EscapeSingleQuotes(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // In single-quoted strings, we need to end the quote, add escaped quote, restart quote
        // 'don't' becomes 'don'\''t'
        return input.Replace("'", "'\\''");
    }
}
