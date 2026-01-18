using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using Siren.Components.Http.Commands;
using Siren.Components.Http.Models;
using Mythetech.Framework.Infrastructure.Commands;

namespace Siren.Components.Http.Consumers
{
    public class RequestImporter : IConsumer<ImportRequest>
    {
        private readonly IFileOpenService _fileOpenService;
        private readonly IMessageBus _bus;
        private readonly ILogger<RequestImporter> _logger;
        private readonly SirenAppState _appState;

        public RequestImporter(
            IFileOpenService fileOpenService,
            IMessageBus bus,
            ILogger<RequestImporter> logger,
            SirenAppState appState)
        {
            _fileOpenService = fileOpenService;
            _bus = bus;
            _logger = logger;
            _appState = appState;
        }

        public async Task Consume(ImportRequest message)
        {
            try
            {
                string? filePath = message.FilePath;

                if (string.IsNullOrEmpty(filePath))
                {
                    var filePaths = await _fileOpenService.OpenFileAsync("Import Request", "json");
                    filePath = filePaths.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);

                SaveRequest? saveRequest;
                try
                {
                    saveRequest = JsonSerializer.Deserialize<SaveRequest>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize request file");
                    await _bus.PublishAsync(new AddNotification("Invalid request file format", Severity.Error));
                    return;
                }

                if (saveRequest == null)
                {
                    _logger.LogWarning("Deserialized request was null");
                    await _bus.PublishAsync(new AddNotification("Invalid request file format", Severity.Error));
                    return;
                }

                var request = ConvertFromCommand(saveRequest);
                var networkRequest = new SirenNetworkRequest
                {
                    Name = saveRequest.RequestUri ?? "Imported Request",
                    Request = request
                };

                _appState.AddNetworkRequest(networkRequest);
                _appState.SetActive(networkRequest.Id);

                if (saveRequest.BodyType == "raw" && !string.IsNullOrEmpty(saveRequest.Body))
                {
                    _appState.ImportRequestBody(saveRequest.Body, saveRequest.BodyType);
                }
                else if (saveRequest.BodyType != "none")
                {
                    _appState.ImportRequestBody(null, saveRequest.BodyType);
                }

                await _bus.PublishAsync(new AddNotification("Request imported successfully", Severity.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing request");
                await _bus.PublishAsync(new AddNotification("Error importing request", Severity.Error));
            }
        }

        private HttpRequest ConvertFromCommand(SaveRequest command)
        {
            var bodyType = command.BodyType switch
            {
                "none" => RequestBodyType.None,
                "raw" => RequestBodyType.Raw,
                "form-data" => RequestBodyType.FormData,
                "binary" => RequestBodyType.Binary,
                _ => RequestBodyType.Raw
            };

            var request = new HttpRequest
            {
                Method = new HttpMethod(command.Method),
                RequestUri = command.RequestUri,
                DisplayUri = command.DisplayUri,
                QueryParameters = command.QueryParameters,
                Headers = command.Headers?.Select(h => new KeyValuePair<string, string>(h.Key, h.Value)).ToList()
                    ?? new List<KeyValuePair<string, string>>(),
                FormData = command.FormData ?? new Dictionary<string, string>(),
                ContentType = command.ContentType,
                Timeout = command.Timeout,
                RetryAttempts = command.RetryAttempts,
                Id = command.Id != Guid.Empty ? command.Id : Guid.NewGuid(),
                BodyType = bodyType,
                RawBody = command.Body ?? ""
            };

            if (bodyType == RequestBodyType.Raw && !string.IsNullOrEmpty(command.Body))
            {
                request.Content = new StringContent(command.Body, System.Text.Encoding.UTF8, command.ContentType);
            }

            return request;
        }
    }
}

