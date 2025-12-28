using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Siren.Components.Collections;
using Siren.Components.Collections.Commands;
using Siren.Components.Collections.Consumers;
using Siren.Components.Http.Models;
using Siren.Components.Shared.Dialogs.Commands;
using Siren.Components.Shared.Notifications.Commands;

namespace Siren.Test.Collections
{
    public class BrunoCollectionImporterTest
    {
        private readonly IFileOpenService _fileOpenService;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<BrunoCollectionImporter> _logger;
        private readonly BrunoCollectionImporter _importer;

        public BrunoCollectionImporterTest()
        {
            _fileOpenService = Substitute.For<IFileOpenService>();
            _messageBus = Substitute.For<IMessageBus>();
            _logger = new NullLogger<BrunoCollectionImporter>();
            _importer = new BrunoCollectionImporter(_fileOpenService, _messageBus, _logger);
        }

        [Fact]
        public async Task Can_Parse_Bruno_Collection_With_Simple_Request()
        {
            var brunoCollection = """
                meta {
                  name: Test Bruno Collection
                  type: http
                  seq: 1
                }

                get {
                  url: https://api.example.com/users
                  body: none
                  auth: none
                }
                """;

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, brunoCollection);

            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new[] { tempFile }));

            await _importer.Consume(new ImportBrunoCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<ShowDialog>(d =>
                d.Dialog == typeof(Siren.Components.Collections.CollectionImportDialog) &&
                d.Title == "Import Bruno Collection"));

            File.Delete(tempFile);
        }

        [Fact]
        public async Task Can_Parse_Bruno_Collection_With_Multiple_Requests()
        {
            var brunoCollection = """
                meta {
                  name: Nested Bruno Collection
                  type: http
                  seq: 1
                }

                get {
                  url: https://api.example.com/items/1
                  body: none
                  auth: none
                }

                post {
                  url: https://api.example.com/items
                  body: {"name":"test"}
                  auth: none
                }
                """;

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, brunoCollection);

            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new[] { tempFile }));

            await _importer.Consume(new ImportBrunoCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<ShowDialog>(d =>
                d.Dialog == typeof(Siren.Components.Collections.CollectionImportDialog) &&
                d.Title == "Import Bruno Collection"));

            File.Delete(tempFile);
        }

        [Fact]
        public async Task Handles_Invalid_Json_Gracefully()
        {
            var invalidJson = "{ invalid json }";
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, invalidJson);

            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new[] { tempFile }));

            await _importer.Consume(new ImportBrunoCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<AddNotification>(n => n.Severity == Severity.Error));
            await _messageBus.DidNotReceive().PublishAsync(Arg.Any<ShowDialog>());

            File.Delete(tempFile);
        }

        [Fact]
        public async Task Handles_Cancelled_File_Selection()
        {
            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(Array.Empty<string>()));

            await _importer.Consume(new ImportBrunoCollection());

            await _messageBus.DidNotReceive().PublishAsync(Arg.Any<ShowDialog>());
        }

        [Fact]
        public async Task Can_Parse_Real_Bruno_Collection_File()
        {
            var testDataPath = Path.Combine("Collections", "TestData", "ChuckNorris Facts.bru");
            var fullPath = Path.Combine(AppContext.BaseDirectory, testDataPath);

            if (!File.Exists(fullPath))
            {
                var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (assemblyLocation != null)
                {
                    fullPath = Path.Combine(assemblyLocation, testDataPath);
                }
            }

            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), testDataPath);
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Test data file not found. Tried: {fullPath}");
            }

            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new[] { fullPath }));

            await _importer.Consume(new ImportBrunoCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<ShowDialog>(d =>
                d.Dialog == typeof(Siren.Components.Collections.CollectionImportDialog) &&
                d.Title == "Import Bruno Collection" &&
                d.Parameters != null &&
                d.Parameters.Any(x => x.Key.Equals("Collection"))));

            var showDialogCalls = _messageBus.ReceivedCalls()
                .Where(c => c.GetMethodInfo().Name == "PublishAsync")
                .Select(c => c.GetArguments().FirstOrDefault())
                .OfType<ShowDialog>()
                .Where(sd => sd.Dialog == typeof(Siren.Components.Collections.CollectionImportDialog))
                .ToList();

            showDialogCalls.Should().NotBeEmpty();
            var dialog = showDialogCalls.First();
            var collection = dialog.Parameters?.FirstOrDefault(x => x.Key.Equals("Collection")).Value as Collection;

            collection.Should().NotBeNull();
            collection!.Name.Should().Be("ChuckNorris Facts");
            collection.Requests.Should().NotBeNull();
            collection.Requests.Should().NotBeEmpty();
            collection.Requests.Should().HaveCount(1);
            collection.Requests[0].Method.Method.Should().Be("GET");
            collection.Requests[0].RequestUri.Should().Be("https://api.chucknorris.io/jokes/random");
        }
    }
}

