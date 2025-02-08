using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Siren.Collections;

namespace Siren.Test
{
    public class CollectionsServiceTest
    {
        private CollectionsService _service;
        public CollectionsServiceTest()
        {
            var factory = new NullLoggerFactory();

            _service = new CollectionsService(factory.CreateLogger<CollectionsService>(), new MockClientFactory());
        }

        [Theory]
        [InlineData("https://petstore3.swagger.io/api/v3/openapi.json")]
        public async Task Can_CreateCollection_FromOpenApiSpec(string specUrl)
        {
            // Act
            var collection = await _service.ImportFromOpenApiSpec(specUrl);

            // Assert
            collection.Should().NotBeNull();
            collection.Requests.Count.Should().BeGreaterThan(0);
            collection.Requests.Any(x => x.Method.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
            collection.Requests.Any(x => x.RequestUri.Contains("petstore3", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        }

        public class MockClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }
    }
}

