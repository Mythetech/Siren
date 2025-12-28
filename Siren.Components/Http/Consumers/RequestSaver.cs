using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.MessageBus;
using Siren.Components.Http.Commands;
using Siren.Components.Shared.Notifications.Commands;

namespace Siren.Components.Http.Consumers
{
    public class RequestSaver : IConsumer<SaveRequest>
    {
        private readonly IFileSaveService _fileSaveService;
        private readonly IMessageBus _bus;
        private readonly ILogger<RequestSaver> _logger;

        public RequestSaver(
            IFileSaveService fileSaveService,
            IMessageBus bus,
            ILogger<RequestSaver> logger)
        {
            _fileSaveService = fileSaveService;
            _bus = bus;
            _logger = logger;
        }

        public async Task Consume(SaveRequest command)
        {
            try
            {
                var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });

                var fileName = GetRequestFileName(command);
                var saveSuccess = await _fileSaveService.SaveFileAsync(fileName, json);

                if (saveSuccess)
                {
                    await _bus.PublishAsync(new AddNotification("Request saved successfully", Severity.Success));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving request");
                await _bus.PublishAsync(new AddNotification("Error saving request", Severity.Error));
            }
        }

        private string GetRequestFileName(SaveRequest command)
        {
            var uri = command.RequestUri ?? "request";
            var sanitized = System.Text.RegularExpressions.Regex.Replace(uri, @"[^\w\s-]", "");
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", "_");
            sanitized = sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
            return $"{sanitized}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.json";
        }
    }
}

