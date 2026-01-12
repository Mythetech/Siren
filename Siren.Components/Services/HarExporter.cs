using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Siren.Components.History;
using Siren.Components.Http.Models;

namespace Siren.Components.Services;

/// <summary>
/// Service for generating HAR (HTTP Archive) format exports from history records.
/// </summary>
public interface IHarExporter
{
    /// <summary>
    /// Generates a HAR JSON string from a list of history records.
    /// </summary>
    string GenerateHar(List<HistoryRecord> records);

    /// <summary>
    /// Generates a HAR JSON string from a single history record.
    /// </summary>
    string GenerateHar(HistoryRecord record);
}

public class HarExporter : IHarExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string GenerateHar(List<HistoryRecord> records)
    {
        var har = new HarArchive
        {
            Log = new HarLog
            {
                Version = "1.2",
                Creator = new HarCreator
                {
                    Name = "Siren HTTP Testing Tool",
                    Version = "1.0"
                },
                Entries = records.Select(CreateEntry).ToList()
            }
        };

        return JsonSerializer.Serialize(har, JsonOptions);
    }

    public string GenerateHar(HistoryRecord record)
    {
        return GenerateHar(new List<HistoryRecord> { record });
    }

    private static HarEntry CreateEntry(HistoryRecord record)
    {
        var entry = new HarEntry
        {
            StartedDateTime = record.Timestamp.ToString("o"),
            Time = record.Response?.Duration.TotalMilliseconds ?? 0,
            Request = CreateRequest(record),
            Response = CreateResponse(record),
            Cache = new HarCache(),
            Timings = CreateTimings(record.Response?.Timeline)
        };

        // Add server IP if available
        if (record.Response?.NetworkInfo?.RemoteAddress != null)
        {
            entry.ServerIPAddress = record.Response.NetworkInfo.RemoteAddress;
        }

        return entry;
    }

    private static HarRequest CreateRequest(HistoryRecord record)
    {
        var request = record.Request;
        var response = record.Response;

        var harRequest = new HarRequest
        {
            Method = record.HttpMethod.Method,
            Url = record.RequestUri,
            HttpVersion = "HTTP/1.1",
            Headers = new List<HarNameValue>(),
            QueryString = new List<HarNameValue>(),
            Cookies = new List<HarCookie>(),
            HeadersSize = response?.RequestSize?.Headers ?? -1,
            BodySize = response?.RequestSize?.Body ?? -1
        };

        // Add headers from actual request headers if available
        if (response?.ActualRequestHeaders != null)
        {
            foreach (var header in response.ActualRequestHeaders)
            {
                harRequest.Headers.Add(new HarNameValue { Name = header.Key, Value = header.Value });
            }
        }
        else if (request?.Headers != null)
        {
            foreach (var header in request.Headers)
            {
                harRequest.Headers.Add(new HarNameValue { Name = header.Key, Value = header.Value });
            }
        }

        // Add query parameters
        if (request?.QueryParameters != null)
        {
            foreach (var param in request.QueryParameters)
            {
                harRequest.QueryString.Add(new HarNameValue { Name = param.Key, Value = param.Value });
            }
        }

        // Add post data if present
        if (request != null && request.BodyType != RequestBodyType.None)
        {
            harRequest.PostData = CreatePostData(request);
        }

        return harRequest;
    }

    private static HarPostData? CreatePostData(HttpRequest request)
    {
        var postData = new HarPostData
        {
            MimeType = request.ContentType ?? "application/octet-stream",
            Params = new List<HarParam>()
        };

        switch (request.BodyType)
        {
            case RequestBodyType.Raw:
                postData.Text = request.RawBody;
                break;

            case RequestBodyType.FormData:
                postData.MimeType = "application/x-www-form-urlencoded";
                if (request.FormData != null)
                {
                    foreach (var kvp in request.FormData)
                    {
                        postData.Params.Add(new HarParam { Name = kvp.Key, Value = kvp.Value });
                    }
                }
                break;

            case RequestBodyType.MultipartFormData:
                postData.MimeType = "multipart/form-data";
                if (request.FormData != null)
                {
                    foreach (var kvp in request.FormData)
                    {
                        postData.Params.Add(new HarParam { Name = kvp.Key, Value = kvp.Value });
                    }
                }
                if (request.BinaryAttachments != null)
                {
                    foreach (var attachment in request.BinaryAttachments)
                    {
                        postData.Params.Add(new HarParam
                        {
                            Name = "file",
                            FileName = attachment.FileName,
                            ContentType = attachment.ContentType
                        });
                    }
                }
                break;

            case RequestBodyType.Binary:
                if (request.BinaryAttachments?.Count > 0)
                {
                    var attachment = request.BinaryAttachments[0];
                    postData.MimeType = attachment.ContentType ?? "application/octet-stream";
                    // For binary, we could include base64 but that bloats the HAR
                    postData.Text = $"[Binary data: {attachment.FileName}, {attachment.Size} bytes]";
                }
                break;
        }

        return postData;
    }

    private static HarResponse CreateResponse(HistoryRecord record)
    {
        var response = record.Response;

        var harResponse = new HarResponse
        {
            Status = (int)record.StatusCode,
            StatusText = record.StatusCode.ToString(),
            HttpVersion = "HTTP/1.1",
            Headers = new List<HarNameValue>(),
            Cookies = new List<HarCookie>(),
            Content = new HarContent(),
            RedirectURL = "",
            HeadersSize = response?.ResponseSize?.Headers ?? -1,
            BodySize = response?.ResponseSize?.Body ?? -1
        };

        // Add response headers
        if (response?.Headers != null)
        {
            foreach (var header in response.Headers)
            {
                harResponse.Headers.Add(new HarNameValue { Name = header.Key, Value = header.Value });
            }
        }

        // Add cookies
        if (response?.Cookies != null)
        {
            foreach (var cookie in response.Cookies)
            {
                harResponse.Cookies.Add(new HarCookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Path = cookie.Path,
                    Domain = cookie.Domain,
                    Expires = cookie.Expires == DateTime.MinValue ? null : cookie.Expires.ToString("o"),
                    HttpOnly = cookie.HttpOnly,
                    Secure = cookie.Secure
                });
            }
        }

        // Add response content
        if (response != null)
        {
            harResponse.Content = new HarContent
            {
                Size = response.ResponseSize?.Body ?? 0,
                MimeType = response.Headers?.GetValueOrDefault("Content-Type") ?? "text/plain",
                Text = response.ResponseText
            };
        }

        return harResponse;
    }

    private static HarTimings CreateTimings(RequestTimeline? timeline)
    {
        if (timeline == null)
        {
            return new HarTimings
            {
                Blocked = -1,
                Dns = -1,
                Connect = -1,
                Ssl = -1,
                Send = -1,
                Wait = -1,
                Receive = -1
            };
        }

        return new HarTimings
        {
            Blocked = 0,
            Dns = timeline.DnsLookup?.TotalMilliseconds ?? -1,
            Connect = timeline.TcpConnection?.TotalMilliseconds ?? -1,
            Ssl = timeline.TlsHandshake?.TotalMilliseconds ?? -1,
            Send = timeline.RequestSent?.TotalMilliseconds ?? -1,
            Wait = timeline.WaitingForResponse?.TotalMilliseconds ?? -1,
            Receive = timeline.ContentDownload?.TotalMilliseconds ?? -1
        };
    }
}

// HAR 1.2 data model classes
public class HarArchive
{
    public HarLog Log { get; set; } = new();
}

public class HarLog
{
    public string Version { get; set; } = "1.2";
    public HarCreator Creator { get; set; } = new();
    public List<HarEntry> Entries { get; set; } = new();
}

public class HarCreator
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
}

public class HarEntry
{
    public string StartedDateTime { get; set; } = "";
    public double Time { get; set; }
    public HarRequest Request { get; set; } = new();
    public HarResponse Response { get; set; } = new();
    public HarCache Cache { get; set; } = new();
    public HarTimings Timings { get; set; } = new();
    public string? ServerIPAddress { get; set; }
}

public class HarRequest
{
    public string Method { get; set; } = "";
    public string Url { get; set; } = "";
    public string HttpVersion { get; set; } = "";
    public List<HarCookie> Cookies { get; set; } = new();
    public List<HarNameValue> Headers { get; set; } = new();
    public List<HarNameValue> QueryString { get; set; } = new();
    public HarPostData? PostData { get; set; }
    public int HeadersSize { get; set; }
    public int BodySize { get; set; }
}

public class HarResponse
{
    public int Status { get; set; }
    public string StatusText { get; set; } = "";
    public string HttpVersion { get; set; } = "";
    public List<HarCookie> Cookies { get; set; } = new();
    public List<HarNameValue> Headers { get; set; } = new();
    public HarContent Content { get; set; } = new();
    public string RedirectURL { get; set; } = "";
    public int HeadersSize { get; set; }
    public int BodySize { get; set; }
}

public class HarCookie
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Path { get; set; }
    public string? Domain { get; set; }
    public string? Expires { get; set; }
    public bool? HttpOnly { get; set; }
    public bool? Secure { get; set; }
}

public class HarNameValue
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

public class HarPostData
{
    public string MimeType { get; set; } = "";
    public List<HarParam> Params { get; set; } = new();
    public string? Text { get; set; }
}

public class HarParam
{
    public string Name { get; set; } = "";
    public string? Value { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

public class HarContent
{
    public int Size { get; set; }
    public string MimeType { get; set; } = "";
    public string? Text { get; set; }
}

public class HarCache
{
}

public class HarTimings
{
    public double Blocked { get; set; }
    public double Dns { get; set; }
    public double Connect { get; set; }
    public double Ssl { get; set; }
    public double Send { get; set; }
    public double Wait { get; set; }
    public double Receive { get; set; }
}
