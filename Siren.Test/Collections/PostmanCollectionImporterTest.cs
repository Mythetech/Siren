using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Siren.Components.Collections;
using Siren.Components.Collections.Commands;
using Siren.Components.Collections.Consumers;
using Siren.Components.Http.Models;
using Siren.Components.Shared.Dialogs.Commands;
using AddNotification = Mythetech.Framework.Infrastructure.Commands.AddNotification;

namespace Siren.Test.Collections
{
    public class PostmanCollectionImporterTest
    {
        private readonly IFileOpenService _fileOpenService;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<PostmanCollectionImporter> _logger;
        private readonly PostmanCollectionImporter _importer;

        public PostmanCollectionImporterTest()
        {
            _fileOpenService = Substitute.For<IFileOpenService>();
            _messageBus = Substitute.For<IMessageBus>();
            _logger = new NullLogger<PostmanCollectionImporter>();
            _importer = new PostmanCollectionImporter(_fileOpenService, _messageBus, _logger);
        }

        [Fact]
        public async Task Can_Parse_Postman_Collection_With_Simple_Request()
        {
            var postmanCollectionJson = """
                {
                    "info": {
                        "name": "Test Collection"
                    },
                    "item": [
                        {
                            "name": "Get Users",
                            "request": {
                                "method": "GET",
                                "url": {
                                    "raw": "https://api.example.com/users",
                                    "protocol": "https",
                                    "host": ["api", "example", "com"],
                                    "path": ["users"]
                                }
                            }
                        }
                    ]
                }
                """;

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, postmanCollectionJson);

            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new[] { tempFile }));

            await _importer.Consume(new ImportPostmanCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<ShowDialog>(d =>
                d.Dialog == typeof(Siren.Components.Collections.CollectionImportDialog) &&
                d.Title == "Import Postman Collection"));

            File.Delete(tempFile);
        }

        [Fact]
        public async Task Can_Parse_Postman_Collection_With_Nested_Folders()
        {
            var postmanCollectionJson = """
                {
                    "info": {
                        "name": "Nested Collection"
                    },
                    "item": [
                        {
                            "name": "Folder 1",
                            "item": [
                                {
                                    "name": "Get Item",
                                    "request": {
                                        "method": "GET",
                                        "url": {
                                            "raw": "https://api.example.com/items/1"
                                        }
                                    }
                                },
                                {
                                    "name": "Create Item",
                                    "request": {
                                        "method": "POST",
                                        "url": {
                                            "raw": "https://api.example.com/items"
                                        },
                                        "body": {
                                            "mode": "raw",
                                            "raw": "{\"name\":\"test\"}"
                                        }
                                    }
                                }
                            ]
                        }
                    ]
                }
                """;

            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, postmanCollectionJson);

            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(new[] { tempFile }));

            await _importer.Consume(new ImportPostmanCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<ShowDialog>(d =>
                d.Dialog == typeof(Siren.Components.Collections.CollectionImportDialog) &&
                d.Title == "Import Postman Collection"));

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

            await _importer.Consume(new ImportPostmanCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<AddNotification>(n => n.Severity == Severity.Error));
            await _messageBus.DidNotReceive().PublishAsync(Arg.Any<ShowDialog>());

            File.Delete(tempFile);
        }

        [Fact]
        public async Task Handles_Cancelled_File_Selection()
        {
            _fileOpenService.OpenFileAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(Array.Empty<string>()));

            await _importer.Consume(new ImportPostmanCollection());

            await _messageBus.DidNotReceive().PublishAsync(Arg.Any<ShowDialog>());
        }

        [Fact]
        public async Task Can_Parse_Real_Postman_Collection_File()
        {
            var testDataPath = Path.Combine("Collections", "TestData", "Public REST APIs.postman_collection.json");
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

            await _importer.Consume(new ImportPostmanCollection());

            await _messageBus.Received().PublishAsync(Arg.Is<ShowDialog>(d =>
                d.Dialog == typeof(Siren.Components.Collections.CollectionImportDialog) &&
                d.Title == "Import Postman Collection" &&
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
            collection!.Name.Should().Be("Public REST APIs");
            collection.Requests.Should().NotBeNull();
            collection.Requests.Should().NotBeEmpty();
            collection.Requests.Should().Contain(r => r.Method.Method == "GET");
        }
    }
}

