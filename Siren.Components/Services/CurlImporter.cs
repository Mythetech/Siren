using System.Text.RegularExpressions;
using Siren.Components.Http.Models;

namespace Siren.Components.Services;

/// <summary>
/// Service for parsing cURL commands into HTTP requests.
/// </summary>
public interface ICurlImporter
{
    HttpRequest? ParseCurl(string curlCommand);
}

public class CurlImporter : ICurlImporter
{
    /// <summary>
    /// Parses a cURL command string into an HttpRequest object.
    /// </summary>
    /// <param name="curlCommand">The cURL command to parse.</param>
    /// <returns>An HttpRequest object, or null if parsing fails.</returns>
    public HttpRequest? ParseCurl(string curlCommand)
    {
        if (string.IsNullOrWhiteSpace(curlCommand))
            return null;

        try
        {
            // Normalize the command - handle line continuations
            var normalized = NormalizeCurlCommand(curlCommand);

            var request = new HttpRequest
            {
                Method = HttpMethod.Get,
                Headers = new List<KeyValuePair<string, string>>(),
                FormData = new Dictionary<string, string>(),
                BinaryAttachments = new List<BinaryAttachment>(),
                BodyType = RequestBodyType.None
            };

            // Extract URL
            var url = ExtractUrl(normalized);
            if (!string.IsNullOrEmpty(url))
            {
                request.RequestUri = url;
            }

            // Extract method
            var method = ExtractMethod(normalized);
            if (!string.IsNullOrEmpty(method))
            {
                request.Method = new HttpMethod(method.ToUpperInvariant());
            }

            // Extract headers
            var headers = ExtractHeaders(normalized);
            foreach (var header in headers)
            {
                request.Headers.Add(header);
            }

            // Extract data/body
            var (body, bodyType, formData) = ExtractBody(normalized);
            if (!string.IsNullOrEmpty(body))
            {
                request.RawBody = body;
                request.BodyType = bodyType;

                // If we have a body but no Content-Type header, default to JSON
                if (bodyType == RequestBodyType.Raw && !headers.Any(h =>
                    string.Equals(h.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)))
                {
                    request.Headers.Add(new KeyValuePair<string, string>("Content-Type", "application/json"));
                }
            }

            if (formData != null && formData.Count > 0)
            {
                request.FormData = formData;
                request.BodyType = RequestBodyType.FormData;
            }

            // If we have data but no explicit method, default to POST
            if (request.BodyType != RequestBodyType.None && request.Method == HttpMethod.Get)
            {
                request.Method = HttpMethod.Post;
            }

            return request;
        }
        catch
        {
            return null;
        }
    }

    private string NormalizeCurlCommand(string command)
    {
        // Remove line continuations (\ followed by newline)
        var normalized = Regex.Replace(command, @"\\\r?\n\s*", " ");
        // Normalize whitespace
        normalized = Regex.Replace(normalized.Trim(), @"\s+", " ");
        return normalized;
    }

    private string? ExtractUrl(string command)
    {
        // Try to find URL after 'curl'
        // URLs can be quoted or unquoted

        // Pattern for quoted URL
        var quotedMatch = Regex.Match(command, @"curl\s+[^""']*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (quotedMatch.Success && Uri.TryCreate(quotedMatch.Groups[1].Value, UriKind.Absolute, out _))
        {
            return quotedMatch.Groups[1].Value;
        }

        // Pattern for unquoted URL - look for http:// or https://
        var urlMatch = Regex.Match(command, @"(https?://[^\s""']+)", RegexOptions.IgnoreCase);
        if (urlMatch.Success)
        {
            // Clean up trailing characters that might not be part of URL
            var url = urlMatch.Groups[1].Value.TrimEnd('\'', '"', '\\');
            return url;
        }

        // Try to find any argument that looks like a URL (after all options)
        var parts = ParseCommandParts(command);
        foreach (var part in parts)
        {
            if (Uri.TryCreate(part, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                return part;
            }
        }

        return null;
    }

    private string? ExtractMethod(string command)
    {
        // -X or --request followed by method
        var match = Regex.Match(command, @"(?:-X|--request)\s+[""']?(\w+)[""']?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    private List<KeyValuePair<string, string>> ExtractHeaders(string command)
    {
        var headers = new List<KeyValuePair<string, string>>();

        // -H or --header followed by "Header: Value"
        var matches = Regex.Matches(command, @"(?:-H|--header)\s+[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            var headerValue = match.Groups[1].Value;
            var colonIndex = headerValue.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = headerValue.Substring(0, colonIndex).Trim();
                var value = headerValue.Substring(colonIndex + 1).Trim();
                headers.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        return headers;
    }

    private (string? body, RequestBodyType bodyType, Dictionary<string, string>? formData) ExtractBody(string command)
    {
        // Check for --data-raw first (raw data, no processing)
        var rawMatch = Regex.Match(command, @"--data-raw\s+[""'](.+?)[""'](?:\s|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (rawMatch.Success)
        {
            return (UnescapeJsonString(rawMatch.Groups[1].Value), RequestBodyType.Raw, null);
        }

        // Check for -d or --data (URL encoded or raw)
        var dataMatch = Regex.Match(command, @"(?:-d|--data)\s+[""'](.+?)[""'](?:\s|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (dataMatch.Success)
        {
            var data = dataMatch.Groups[1].Value;

            // Check if it looks like JSON
            if (data.TrimStart().StartsWith("{") || data.TrimStart().StartsWith("["))
            {
                return (UnescapeJsonString(data), RequestBodyType.Raw, null);
            }

            // Check if it looks like form data (key=value&key=value)
            if (data.Contains("=") && !data.Contains("{"))
            {
                var formData = ParseFormData(data);
                return (null, RequestBodyType.FormData, formData);
            }

            return (data, RequestBodyType.Raw, null);
        }

        // Check for --data-urlencode
        var urlEncodeMatches = Regex.Matches(command, @"--data-urlencode\s+[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (urlEncodeMatches.Count > 0)
        {
            var formData = new Dictionary<string, string>();
            foreach (Match match in urlEncodeMatches)
            {
                var part = match.Groups[1].Value;
                var eqIndex = part.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = part.Substring(0, eqIndex);
                    var value = part.Substring(eqIndex + 1);
                    formData[key] = value;
                }
            }
            return (null, RequestBodyType.FormData, formData);
        }

        // Check for -F or --form (multipart form data)
        var formMatches = Regex.Matches(command, @"(?:-F|--form)\s+[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (formMatches.Count > 0)
        {
            var formData = new Dictionary<string, string>();
            foreach (Match match in formMatches)
            {
                var part = match.Groups[1].Value;
                var eqIndex = part.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = part.Substring(0, eqIndex);
                    var value = part.Substring(eqIndex + 1);
                    // Skip file uploads (start with @)
                    if (!value.StartsWith("@"))
                    {
                        formData[key] = value;
                    }
                }
            }
            return (null, RequestBodyType.MultipartFormData, formData);
        }

        return (null, RequestBodyType.None, null);
    }

    private Dictionary<string, string> ParseFormData(string data)
    {
        var result = new Dictionary<string, string>();
        var pairs = data.Split('&');
        foreach (var pair in pairs)
        {
            var eqIndex = pair.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = Uri.UnescapeDataString(pair.Substring(0, eqIndex));
                var value = Uri.UnescapeDataString(pair.Substring(eqIndex + 1));
                result[key] = value;
            }
        }
        return result;
    }

    private string UnescapeJsonString(string input)
    {
        // Handle common escapes in curl commands
        return input
            .Replace("\\\"", "\"")
            .Replace("\\'", "'")
            .Replace("\\n", "\n")
            .Replace("\\t", "\t")
            .Replace("\\\\", "\\");
    }

    private List<string> ParseCommandParts(string command)
    {
        var parts = new List<string>();
        var current = "";
        var inQuote = false;
        var quoteChar = ' ';

        foreach (var c in command)
        {
            if (!inQuote && (c == '"' || c == '\''))
            {
                inQuote = true;
                quoteChar = c;
            }
            else if (inQuote && c == quoteChar)
            {
                inQuote = false;
                if (!string.IsNullOrEmpty(current))
                {
                    parts.Add(current);
                    current = "";
                }
            }
            else if (!inQuote && c == ' ')
            {
                if (!string.IsNullOrEmpty(current))
                {
                    parts.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            parts.Add(current);
        }

        return parts;
    }
}
