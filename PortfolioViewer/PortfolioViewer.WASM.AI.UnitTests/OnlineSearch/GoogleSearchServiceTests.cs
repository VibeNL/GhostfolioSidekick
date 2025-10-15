using System.Net;
using System.Text;
using System.Text.Json;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch;
using Moq;
using Moq.Protected;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.OnlineSearch
{
    public class GoogleSearchServiceTests
    {
        private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responder, Uri? baseAddress = null)
        {
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => responder(req));

            var client = new HttpClient(mockHandler.Object);
            if (baseAddress is not null)
                client.BaseAddress = baseAddress;
            return client;
        }

        [Fact]
        public async Task SearchAsync_WithEmptyQuery_ReturnsError()
        {
            // Arrange
            var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var service = new GoogleSearchService(httpClient);

            var request = new GoogleSearchRequest { Query = "   " };

            // Act
            var response = await service.SearchAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Search query cannot be empty", response.ErrorMessage);
        }

        [Fact]
        public async Task SearchAsync_BackendFails_ReturnsError()
        {
            // Arrange
            var httpClient = CreateHttpClient(req => new HttpResponseMessage(HttpStatusCode.InternalServerError), new Uri("http://localhost"));
            var service = new GoogleSearchService(httpClient);

            var request = new GoogleSearchRequest { Query = "test" };

            // Act
            var response = await service.SearchAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("Search request failed", response.ErrorMessage);
        }

        [Fact]
        public async Task SearchAsync_NoResults_ReturnsEmptyResults()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new { items = Array.Empty<object>() });
            var httpClient = CreateHttpClient(req =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }, new Uri("http://localhost"));

            var service = new GoogleSearchService(httpClient);

            var request = new GoogleSearchRequest { Query = "noresults" };

            // Act
            var response = await service.SearchAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Empty(response.Results);
        }

        [Fact]
        public async Task SearchAsync_WithResults_ReturnsWebResultsWithContent()
        {
            // Arrange
            var searchResult = new
            {
                items = new[]
                {
                    new { title = "Title 1", link = "http://example.com/page", snippet = "Snippet 1" }
                }
            };

            var searchJson = JsonSerializer.Serialize(searchResult);
            var pageContent = "<html>page content</html>";

            var httpClient = CreateHttpClient(req =>
            {
                // Distinguish between search and proxy fetch by path
                if (req.RequestUri!.AbsolutePath.Contains("google-search"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(searchJson, Encoding.UTF8, "application/json")
                    };
                }

                if (req.RequestUri!.AbsolutePath.Contains("proxy/fetch"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(pageContent, Encoding.UTF8, "text/html")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }, new Uri("http://localhost"));

            var service = new GoogleSearchService(httpClient);

            var request = new GoogleSearchRequest { Query = "hasresults" };

            // Act
            var response = await service.SearchAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Single(response.Results);
            var result = response.Results.First();
            Assert.Equal("Title 1", result.Title);
            Assert.Equal("http://example.com/page", result.Link);
            Assert.Equal("Snippet 1", result.Snippet);
            Assert.Equal(pageContent, result.Content);
        }

        [Fact]
        public async Task SearchAsync_StringOverload_ReturnsCollection()
        {
            // Arrange
            var searchResult = new
            {
                items = new[]
                {
                    new { title = "T", link = "http://x", snippet = "S" }
                }
            };

            var searchJson = JsonSerializer.Serialize(searchResult);

            var httpClient = CreateHttpClient(req =>
            {
                if (req.RequestUri!.AbsolutePath.Contains("google-search"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(searchJson, Encoding.UTF8, "application/json")
                    };
                }

                // For fetch calls return 404 so content will be null
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }, new Uri("http://localhost"));

            var service = new GoogleSearchService(httpClient);

            // Act
            var results = await service.SearchAsync("query-string");

            // Assert
            Assert.Single(results);
            var item = results.First();
            Assert.Equal("T", item.Title);
            Assert.Equal("http://x", item.Link);
            Assert.Equal("S", item.Snippet);
            Assert.Null(item.Content);
        }

        [Fact]
        public async Task GetWebsiteContentAsync_NullOrEmpty_ReturnsNull()
        {
            // Arrange
            var httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var service = new GoogleSearchService(httpClient);

            // Act & Assert
            Assert.Null(await service.GetWebsiteContentAsync(null));
            Assert.Null(await service.GetWebsiteContentAsync(""));
        }

        [Fact]
        public async Task GetWebsiteContentAsync_NonSuccess_ReturnsNull()
        {
            // Arrange
            var httpClient = CreateHttpClient(req => new HttpResponseMessage(HttpStatusCode.NotFound), new Uri("http://localhost"));
            var service = new GoogleSearchService(httpClient);

            // Act
            var result = await service.GetWebsiteContentAsync("http://example.com");

            // Assert
            Assert.Null(result);
        }
    }
}
