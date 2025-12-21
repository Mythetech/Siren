using System;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Siren.Components.History;
using Siren.Components.Http;
using Siren.Components.Settings;
using Siren.Components.RequestContextPanel.Authentication;
namespace Siren.Components.Services
{
    public interface IHttpRequestClient
    {
        public Task<RequestResult> SendHttpRequestAsync(HttpRequest request, CancellationToken ct);
    }

    public class HttpRequestClient : IHttpRequestClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHistoryService _historyService;
        private readonly ILogger<HttpRequestClient> _logger;
        private readonly SettingsState _settings;
        private readonly ICookieService _cookieService;
        private readonly RequestAuthenticationState _authState;

        public HttpRequestClient(IHttpClientFactory httpClientFactory, IHistoryService historyService, ILogger<HttpRequestClient> logger, SettingsState settings, ICookieService cookieService, RequestAuthenticationState authState)
        {
            _httpClientFactory = httpClientFactory;
            _historyService = historyService;
            _logger = logger;
            _settings = settings;
            _cookieService = cookieService;
            _authState = authState;
        }

        public async Task<RequestResult> SendHttpRequestAsync(HttpRequest request, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient();
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = request.Method,
                RequestUri = new Uri(request.RequestUri),
                Content = request.Content,
            };

            if (request?.Headers != null)
            {
                foreach (var header in request.Headers)
                {
                    httpRequestMessage.Headers.Add(header.Key, header.Value);
                }
            }

            AddSystemHeadersToRequest(httpRequestMessage);
            AddCookiesToRequest(httpRequestMessage);
            AddAuthenticationToRequest(httpRequestMessage);

            var actualRequestHeaders = CaptureActualRequestHeaders(httpRequestMessage, client);

            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage? response;
            RequestResult? result = null!;

            try
            {
                response = await client.SendAsync(httpRequestMessage, ct);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                result = new RequestResult
                {
                    Error = ex,
                    Duration = stopwatch.Elapsed,
                    ActualRequestHeaders = actualRequestHeaders
                };

                return result;
            }
            stopwatch.Stop();

            string sc = "";
            if (response != null)
            {
                sc = await response?.Content?.ReadAsStringAsync(CancellationToken.None) ?? "";
            }

            result ??= new RequestResult
            {
                HttpStatusCode = response.StatusCode,
                StatusCode = (int)response.StatusCode,
                Cookies = _cookieService.ParseCookies(response),
                Duration = stopwatch.Elapsed,
                ResponseContent = response.Content,
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault()),
                ActualRequestHeaders = actualRequestHeaders
            };

            if (response?.Content != null)
            {
                result.ResponseSize = new HttpPayloadSize(
                    Body: (int)(response.Content.Headers.ContentLength ?? 0),
                    Headers: (int)response.Content.Headers.ToString().Length
                );
            }

            CreateHistoryRecord(request, result, sc);

            return result;
        }

        private void CreateHistoryRecord(HttpRequest req, RequestResult result, string responseText = "")
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                try
                {
                    responseText = System.Text.Json.JsonSerializer.Serialize(System.Text.Json.JsonSerializer.Deserialize<dynamic>(result?.ResponseContent?.ReadAsStream()), new System.Text.Json.JsonSerializerOptions() { WriteIndented = true }) ?? result?.Error?.Message ?? "No Content";
                }
                catch (Exception ex)
                {

                    _logger.LogError(ex, "Couldn't deserialize response to json");
                }
            }

            if (result != null)
                result.ResponseText = responseText;

            var record = new HistoryRecord()
            {
                HttpMethod = req.Method,
                RequestId = req.Id,
                RequestUri = req.RequestUri,
                StatusCode = (HttpStatusCode)result.StatusCode,
                Request = req,
                Response = result,
                DisplayText = responseText,
            };

            if (!_settings.SaveHttpContent)
            {
                record.Request.Content = null;
                record.Response = null;
                record.DisplayText = "";
            }

            _historyService.AddHistoryRecord(record);
        }

        private void AddCookiesToRequest(HttpRequestMessage request)
        {
            var requestUri = request.RequestUri;

            var applicableCookies = _cookieService.GetCookies().Where(c =>
                (string.IsNullOrEmpty(c.Domain) || c.Domain == requestUri.Host) &&
                (string.IsNullOrEmpty(c.Path) || requestUri.AbsolutePath.StartsWith(c.Path)) &&
                (!(c.Expires == default) || c.Expires > DateTimeOffset.Now) &&
                (!c.Secure || requestUri.Scheme == Uri.UriSchemeHttps));

            if (applicableCookies.Any())
            {
                var cookieHeader = string.Join("; ", applicableCookies.Select(c => $"{c.Name}={c.Value}"));
                request.Headers.Add("Cookie", cookieHeader);
            }
        }

        private void AddAuthenticationToRequest(HttpRequestMessage request)
        {
            switch (_authState.AuthType)
            {
                case AuthenticationType.Bearer:
                    if (_authState.AuthParams.TryGetValue("token", out var token))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                    break;

                case AuthenticationType.Basic:
                    if (_authState.AuthParams.TryGetValue("username", out var username) &&
                        _authState.AuthParams.TryGetValue("password", out var password))
                    {
                        var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                    }
                    break;

                case AuthenticationType.ApiKey:
                    if (_authState.AuthParams.TryGetValue("apiKeyName", out var keyName) &&
                        _authState.AuthParams.TryGetValue("apiKeyValue", out var keyValue))
                    {
                        request.Headers.Add(keyName, keyValue);
                    }
                    break;
            }
        }

        private void AddSystemHeadersToRequest(HttpRequestMessage request)
        {
            if (_settings.SendRequestsWithSystemToken)
            {
                request.Headers.Add("x-siren-request-id", Guid.NewGuid().ToString());
            }
        }

        private Dictionary<string, string> CaptureActualRequestHeaders(HttpRequestMessage request, HttpClient client)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in request.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
            }

            if (request.Headers.Authorization != null && !headers.ContainsKey("Authorization"))
            {
                headers["Authorization"] = request.Headers.Authorization.ToString();
            }

            if (client.DefaultRequestHeaders != null)
            {
                foreach (var header in client.DefaultRequestHeaders)
                {
                    if (!headers.ContainsKey(header.Key))
                    {
                        headers[header.Key] = string.Join(", ", header.Value);
                    }
                }
            }

            return headers;
        }
    }
}

