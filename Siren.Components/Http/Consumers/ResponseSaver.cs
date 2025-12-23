using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Components.Infrastructure;
using Mythetech.Components.Infrastructure.MessageBus;
using Siren.Components.Http.Commands;
using Siren.Components.Shared.Notifications.Commands;

namespace Siren.Components.Http.Consumers
{
    public class ResponseSaver : IConsumer<SaveResponse>
    {
        private readonly IFileSaveService _fileSaveService;
        private readonly IMessageBus _bus;
        private readonly ILogger<ResponseSaver> _logger;

        public ResponseSaver(
            IFileSaveService fileSaveService,
            IMessageBus bus,
            ILogger<ResponseSaver> logger)
        {
            _fileSaveService = fileSaveService;
            _bus = bus;
            _logger = logger;
        }

        public async Task Consume(SaveResponse command)
        {
            try
            {
                var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var fileName = $"response_{command.Timestamp:yyyyMMdd_HHmmss}.json";
                var saveSuccess = await _fileSaveService.SaveFileAsync(fileName, json);

                if (saveSuccess)
                {
                    await _bus.PublishAsync(new AddNotification("Response saved successfully", Severity.Success));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving response");
                await _bus.PublishAsync(new AddNotification("Error saving response", Severity.Error));
            }
        }
    }
}

