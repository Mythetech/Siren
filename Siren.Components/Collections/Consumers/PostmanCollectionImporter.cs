using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using Siren.Components.Collections.Commands;
using Siren.Components.Http.Models;
using Siren.Components.Shared.Dialogs.Commands;
using Mythetech.Framework.Infrastructure.Commands;

namespace Siren.Components.Collections.Consumers
{
    public class PostmanCollectionImporter : IConsumer<ImportPostmanCollection>
    {
        private readonly IFileOpenService _fileOpenService;
        private readonly IMessageBus _bus;
        private readonly ILogger<PostmanCollectionImporter> _logger;

        public PostmanCollectionImporter(
            IFileOpenService fileOpenService,
            IMessageBus bus,
            ILogger<PostmanCollectionImporter> logger)
        {
            _fileOpenService = fileOpenService;
            _bus = bus;
            _logger = logger;
        }

        public async Task Consume(ImportPostmanCollection message)
        {
            try
            {
                string? filePath = message.FilePath;

                if (string.IsNullOrEmpty(filePath))
                {
                    var filePaths = await _fileOpenService.OpenFileAsync("Import Postman Collection", "json");
                    filePath = filePaths.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);

                PostmanCollection? postmanCollection;
                try
                {
                    postmanCollection = JsonSerializer.Deserialize<PostmanCollection>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize Postman collection file");
                    await _bus.PublishAsync(new AddNotification("Invalid Postman collection file format", Severity.Error));
                    return;
                }

                if (postmanCollection == null || postmanCollection.Info == null)
                {
                    _logger.LogWarning("Deserialized Postman collection was null or invalid");
                    await _bus.PublishAsync(new AddNotification("Invalid Postman collection file format", Severity.Error));
                    return;
                }

                var requests = ExtractRequests(postmanCollection);
                var collection = Collection.Create(postmanCollection.Info.Name ?? "Imported Postman Collection", requests.ToArray());

                var options = new DialogOptions { CloseOnEscapeKey = true, BackgroundClass = "siren-dialog" };
                var parameters = new DialogParameters();
                parameters.Add("Collection", collection);

                await _bus.PublishAsync(new Siren.Components.Shared.Dialogs.Commands.ShowDialog(typeof(CollectionImportDialog), "Import Postman Collection", options, parameters));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Postman collection");
                await _bus.PublishAsync(new AddNotification("Error importing Postman collection", Severity.Error));
            }
        }

        private List<HttpRequest> ExtractRequests(PostmanCollection collection)
        {
            var requests = new List<HttpRequest>();
            ExtractRequestsFromItems(collection.Item ?? new List<PostmanItem>(), requests);
            return requests;
        }

        private void ExtractRequestsFromItems(List<PostmanItem> items, List<HttpRequest> requests)
        {
            foreach (var item in items)
            {
                if (item.Request != null)
                {
                    var httpRequest = ConvertToHttpRequest(item);
                    if (httpRequest != null)
                    {
                        requests.Add(httpRequest);
                    }
                }

                if (item.Item != null && item.Item.Any())
                {
                    ExtractRequestsFromItems(item.Item, requests);
                }
            }
        }

        private HttpRequest? ConvertToHttpRequest(PostmanItem item)
        {
            if (item.Request == null)
                return null;

            var request = item.Request;
            var method = new HttpMethod(request.Method ?? "GET");
            var url = request.Url;

            string requestUri = "";
            string displayUri = "";

            if (url != null)
            {
                if (url.Raw != null)
                {
                    requestUri = url.Raw;
                    displayUri = url.Raw;
                }
                else if (url.Host != null && url.Path != null)
                {
                    var protocol = url.Protocol ?? "https";
                    var host = string.Join(".", url.Host ?? new List<string>());
                    var path = string.Join("/", url.Path ?? new List<string>());
                    var query = url.Query != null && url.Query.Any()
                        ? "?" + string.Join("&", url.Query.Select(q => $"{q.Key}={q.Value}"))
                        : "";
                    requestUri = $"{protocol}://{host}/{path}{query}";
                    displayUri = $"/{path}{query}";
                }
            }

            var bodyType = RequestBodyType.None;
            var rawBody = "";
            var formData = new Dictionary<string, string>();
            var contentType = "application/json";

            if (request.Body != null)
            {
                if (request.Body.Mode == "raw" && request.Body.Raw != null)
                {
                    bodyType = RequestBodyType.Raw;
                    rawBody = request.Body.Raw;
                }
                else if (request.Body.Mode == "urlencoded" && request.Body.Urlencoded != null)
                {
                    bodyType = RequestBodyType.FormData;
                    formData = request.Body.Urlencoded
                        .Where(u => u.Key != null && u.Value != null)
                        .ToDictionary(u => u.Key!, u => u.Value!);
                    contentType = "application/x-www-form-urlencoded";
                }
                else if (request.Body.Mode == "formdata" && request.Body.Formdata != null)
                {
                    bodyType = RequestBodyType.FormData;
                    formData = request.Body.Formdata
                        .Where(f => f.Key != null && f.Value != null && f.Type == "text")
                        .ToDictionary(f => f.Key!, f => f.Value!);
                    contentType = "multipart/form-data";
                }
            }

            var httpRequest = new HttpRequest
            {
                Method = method,
                RequestUri = requestUri,
                DisplayUri = displayUri,
                Headers = request.Header?.Select(h => new KeyValuePair<string, string>(h.Key ?? "", h.Value ?? "")).ToList()
                    ?? new List<KeyValuePair<string, string>>(),
                ContentType = contentType,
                FormData = formData,
                BodyType = bodyType,
                RawBody = rawBody
            };

            if (bodyType == RequestBodyType.Raw && !string.IsNullOrEmpty(rawBody))
            {
                httpRequest.Content = new StringContent(rawBody, System.Text.Encoding.UTF8, contentType);
            }

            return httpRequest;
        }

        private class PostmanCollection
        {
            public PostmanInfo? Info { get; set; }
            public List<PostmanItem>? Item { get; set; }
        }

        private class PostmanInfo
        {
            public string? Name { get; set; }
        }

        private class PostmanItem
        {
            public string? Name { get; set; }
            public PostmanRequest? Request { get; set; }
            public List<PostmanItem>? Item { get; set; }
        }

        private class PostmanRequest
        {
            public string? Method { get; set; }
            public List<PostmanHeader>? Header { get; set; }
            public PostmanUrl? Url { get; set; }
            public PostmanBody? Body { get; set; }
        }

        private class PostmanHeader
        {
            public string? Key { get; set; }
            public string? Value { get; set; }
        }

        private class PostmanUrl
        {
            public string? Raw { get; set; }
            public string? Protocol { get; set; }
            public List<string>? Host { get; set; }
            public List<string>? Path { get; set; }
            public List<PostmanQuery>? Query { get; set; }
        }

        private class PostmanQuery
        {
            public string? Key { get; set; }
            public string? Value { get; set; }
        }

        private class PostmanBody
        {
            public string? Mode { get; set; }
            public string? Raw { get; set; }
            public List<PostmanUrlencoded>? Urlencoded { get; set; }
            public List<PostmanFormdata>? Formdata { get; set; }
        }

        private class PostmanUrlencoded
        {
            public string? Key { get; set; }
            public string? Value { get; set; }
        }

        private class PostmanFormdata
        {
            public string? Key { get; set; }
            public string? Value { get; set; }
            public string? Type { get; set; }
        }
    }
}

