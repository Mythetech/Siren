using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Components.Infrastructure;
using Mythetech.Components.Infrastructure.MessageBus;
using Siren.Components.Collections.Commands;
using Siren.Components.Http.Models;
using Siren.Components.Shared.Dialogs;
using Siren.Components.Shared.Dialogs.Commands;
using Siren.Components.Shared.Notifications.Commands;

namespace Siren.Components.Collections.Consumers
{
    public class BrunoCollectionImporter : IConsumer<ImportBrunoCollection>
    {
        private readonly IFileOpenService _fileOpenService;
        private readonly IMessageBus _bus;
        private readonly ILogger<BrunoCollectionImporter> _logger;

        public BrunoCollectionImporter(
            IFileOpenService fileOpenService,
            IMessageBus bus,
            ILogger<BrunoCollectionImporter> logger)
        {
            _fileOpenService = fileOpenService;
            _bus = bus;
            _logger = logger;
        }

        public async Task Consume(ImportBrunoCollection message)
        {
            try
            {
                string? filePath = message.FilePath;

                if (string.IsNullOrEmpty(filePath))
                {
                    var filePaths = await _fileOpenService.OpenFileAsync("Import Bruno Collection", "bru");
                    filePath = filePaths.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return;
                }

                var fileContent = await File.ReadAllTextAsync(filePath);
                var brunoCollection = ParseBrunoFile(fileContent, filePath);

                if (brunoCollection == null || string.IsNullOrEmpty(brunoCollection.Name) || brunoCollection.Items == null)
                {
                    _logger.LogWarning("Failed to parse Bruno collection file");
                    await _bus.PublishAsync(new AddNotification("Invalid Bruno collection file format", Severity.Error));
                    return;
                }

                var requests = ExtractRequests(brunoCollection);
                var collection = Collection.Create(brunoCollection.Name, requests.ToArray());

                var options = SirenDialogs.CreateDefaultOptions(MaxWidth.Medium);
                var parameters = new DialogParameters();
                parameters.Add("Collection", collection);

                await _bus.PublishAsync(new ShowDialog(typeof(CollectionImportDialog), "Import Bruno Collection", options, parameters));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Bruno collection");
                await _bus.PublishAsync(new AddNotification("Error importing Bruno collection", Severity.Error));
            }
        }

        private BrunoCollection? ParseBrunoFile(string content, string filePath)
        {
            try
            {
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                var collection = new BrunoCollection();
                List<BrunoItem>? items = null;
                BrunoItem? currentItem = null;
                var inMeta = false;
                var inRequest = false;
                var inAssert = false;
                var inTests = false;
                var braceDepth = 0;
                string? currentMethod = null;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    if (trimmed.StartsWith("meta", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentItem != null)
                        {
                            items ??= [];
                            items.Add(currentItem);
                            currentItem = null;
                        }
                        inMeta = true;
                        inRequest = false;
                        inAssert = false;
                        inTests = false;
                        braceDepth = 0;
                        currentMethod = null;
                        continue;
                    }

                    if (trimmed.StartsWith("assert", StringComparison.OrdinalIgnoreCase))
                    {
                        inMeta = false;
                        inRequest = false;
                        inAssert = true;
                        inTests = false;
                        braceDepth = 0;
                        continue;
                    }

                    if (trimmed.StartsWith("tests", StringComparison.OrdinalIgnoreCase))
                    {
                        inMeta = false;
                        inRequest = false;
                        inAssert = false;
                        inTests = true;
                        braceDepth = 0;
                        continue;
                    }

                    var methodMatch = GetHttpMethod(trimmed);
                    if (methodMatch != null)
                    {
                        if (currentItem != null)
                        {
                            items ??= [];
                            items.Add(currentItem);
                        }
                        currentMethod = methodMatch;
                        currentItem = new BrunoItem
                        {
                            Name = methodMatch,
                            Type = "http",
                            Request = new BrunoRequest { Method = methodMatch }
                        };
                        inMeta = false;
                        inRequest = true;
                        inAssert = false;
                        inTests = false;
                        braceDepth = 0;
                        continue;
                    }

                    if (trimmed == "{")
                    {
                        braceDepth++;
                        continue;
                    }

                    if (trimmed == "}")
                    {
                        braceDepth--;
                        if (braceDepth == 0)
                        {
                            if (inRequest && currentItem != null)
                            {
                                items ??= [];
                                items.Add(currentItem);
                                currentItem = null;
                                currentMethod = null;
                            }
                            inMeta = false;
                            inRequest = false;
                            inAssert = false;
                            inTests = false;
                        }
                        continue;
                    }

                    if (inMeta && trimmed.Contains(":"))
                    {
                        var parts = trimmed.Split(new[] { ':' }, 2, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
                            {
                                collection.Name = value;
                            }
                        }
                    }
                    else if (inRequest && currentItem != null && currentItem.Request != null && trimmed.Contains(":"))
                    {
                        var parts = trimmed.Split(new[] { ':' }, 2, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();
                            
                            switch (key.ToLower())
                            {
                                case "url":
                                    currentItem.Request.Url = value;
                                    break;
                                case "body":
                                    if (value != "none" && !string.IsNullOrWhiteSpace(value))
                                    {
                                        currentItem.Request.Body = new BrunoBody { Mode = "raw", Text = value };
                                    }
                                    break;
                                case "auth":
                                    break;
                            }
                        }
                    }
                }

                if (currentItem != null)
                {
                    items ??= [];
                    items.Add(currentItem);
                }

                if (string.IsNullOrEmpty(collection.Name))
                {
                    collection.Name = Path.GetFileNameWithoutExtension(filePath);
                }

                collection.Items = items;
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Bruno file");
                return null;
            }
        }

        private string? GetHttpMethod(string line)
        {
            var trimmed = line.Trim().ToLower();
            if (trimmed == "get" || trimmed.StartsWith("get "))
                return "GET";
            if (trimmed == "post" || trimmed.StartsWith("post "))
                return "POST";
            if (trimmed == "put" || trimmed.StartsWith("put "))
                return "PUT";
            if (trimmed == "delete" || trimmed.StartsWith("delete "))
                return "DELETE";
            if (trimmed == "patch" || trimmed.StartsWith("patch "))
                return "PATCH";
            if (trimmed == "head" || trimmed.StartsWith("head "))
                return "HEAD";
            if (trimmed == "options" || trimmed.StartsWith("options "))
                return "OPTIONS";
            return null;
        }


        private List<HttpRequest> ExtractRequests(BrunoCollection collection)
        {
            var requests = new List<HttpRequest>();
            ExtractRequestsFromItems(collection.Items ?? new List<BrunoItem>(), requests);
            return requests;
        }

        private void ExtractRequestsFromItems(List<BrunoItem> items, List<HttpRequest> requests)
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

                if (item.Items != null && item.Items.Any())
                {
                    ExtractRequestsFromItems(item.Items, requests);
                }
            }
        }

        private HttpRequest? ConvertToHttpRequest(BrunoItem item)
        {
            if (item.Request == null)
                return null;

            var request = item.Request;
            var methodString = request.Method ?? item.Name ?? "GET";
            var method = new HttpMethod(methodString);

            string requestUri = request.Url ?? "";
            string displayUri = request.Url ?? "";

            if (request.Params != null && request.Params.Any())
            {
                var queryString = string.Join("&", request.Params.Select(p => $"{p.Name}={p.Value}"));
                requestUri += (requestUri.Contains("?") ? "&" : "?") + queryString;
                displayUri = requestUri;
            }

            var bodyType = RequestBodyType.None;
            var rawBody = "";

            if (request.Body != null && !string.IsNullOrEmpty(request.Body.Text) && request.Body.Text != "none")
            {
                bodyType = RequestBodyType.Raw;
                rawBody = request.Body.Text;
            }
            else if (request.Body?.Mode == "formdata" || (request.Body?.Params != null && request.Body.Params.Any()))
            {
                bodyType = RequestBodyType.FormData;
            }

            var httpRequest = new HttpRequest
            {
                Method = method,
                RequestUri = requestUri,
                DisplayUri = displayUri,
                Headers = request.Headers?.Select(h => new KeyValuePair<string, string>(h.Name ?? "", h.Value ?? "")).ToList()
                    ?? new List<KeyValuePair<string, string>>(),
                ContentType = "application/json",
                BodyType = bodyType,
                RawBody = rawBody
            };

            if (bodyType == RequestBodyType.Raw && !string.IsNullOrEmpty(rawBody))
            {
                httpRequest.Content = new StringContent(rawBody, System.Text.Encoding.UTF8, httpRequest.ContentType);
            }

            return httpRequest;
        }

        private class BrunoCollection
        {
            public string? Name { get; set; }
            public List<BrunoItem>? Items { get; set; }
        }

        private class BrunoItem
        {
            public string? Name { get; set; }
            public string? Type { get; set; }
            public BrunoRequest? Request { get; set; }
            public List<BrunoItem>? Items { get; set; }
        }

        private class BrunoRequest
        {
            public string? Method { get; set; }
            public string? Url { get; set; }
            public List<BrunoHeader>? Headers { get; set; }
            public List<BrunoParam>? Params { get; set; }
            public BrunoBody? Body { get; set; }
        }

        private class BrunoHeader
        {
            public string? Name { get; set; }
            public string? Value { get; set; }
        }

        private class BrunoParam
        {
            public string? Name { get; set; }
            public string? Value { get; set; }
        }

        private class BrunoBody
        {
            public string? Mode { get; set; }
            public string? Text { get; set; }
            public List<BrunoBodyParam>? Params { get; set; }
        }

        private class BrunoBodyParam
        {
            public string? Name { get; set; }
            public string? Value { get; set; }
            public string? Type { get; set; }
        }
    }
}

