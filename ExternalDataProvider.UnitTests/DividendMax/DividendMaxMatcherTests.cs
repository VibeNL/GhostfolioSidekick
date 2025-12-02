using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GhostfolioSidekick.ExternalDataProvider.DividendMax;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Moq;
using Moq.Protected;
using Xunit;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.DividendMax
{
    public class DividendMaxMatcherTests
    {
        [Fact]
        public async Task MatchSymbol_ReturnsProfile_WhenBestMatchFound()
        {
            // Arrange
            var suggestJson = "[{\"Id\":1,\"Name\":\"Apple Inc\",\"Path\":\"/stocks/us/apple-inc-aapl\",\"Ticker\":\"AAPL\",\"Flag\":\"US\"}]";
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(suggestJson) });
            var httpClient = new HttpClient(handlerMock.Object);
            var matcher = new DividendMaxMatcher(httpClient);
            var identifiers = new[] {
                PartialSymbolIdentifier.CreateGeneric("AAPL"),
                PartialSymbolIdentifier.CreateGeneric("Apple")
            };

            // Act
            var result = await matcher.MatchSymbol(identifiers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("AAPL", result.Symbol);
            Assert.Equal("Apple Inc", result.Name);
            Assert.Equal("DividendMax", result.DataSource);
            Assert.Contains("AAPL", result.Identifiers);
            Assert.Contains("Apple", result.Identifiers);
            Assert.Equal("https://www.dividendmax.com/stocks/us/apple-inc-aapl", result.WebsiteUrl);
        }

        [Fact]
        public async Task MatchSymbol_ReturnsNull_WhenNoResults()
        {
            // Arrange
            var suggestJson = "[]";
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(suggestJson) });
            var httpClient = new HttpClient(handlerMock.Object);
            var matcher = new DividendMaxMatcher(httpClient);
            var identifiers = new[] { PartialSymbolIdentifier.CreateGeneric("ZZZZ") };

            // Act
            var result = await matcher.MatchSymbol(identifiers);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MatchSymbol_ReturnsNull_WhenScoreIsZero()
        {
            // Arrange
            // This test depends on SemanticMatcher.CalculateSemanticMatchScore returning 0 for unrelated symbols.
            // If you want to control the score, refactor SemanticMatcher to be injectable.
            var suggestJson = "[{\"Id\":2,\"Name\":\"Not Related\",\"Path\":\"/stocks/us/not-related\",\"Ticker\":\"NR\",\"Flag\":\"US\"}]";
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(suggestJson) });
            var httpClient = new HttpClient(handlerMock.Object);
            var matcher = new DividendMaxMatcher(httpClient);
            var identifiers = new[] { PartialSymbolIdentifier.CreateGeneric("AAPL") };

            // Act
            var result = await matcher.MatchSymbol(identifiers);

            // Assert
            Assert.Null(result);
        }
    }
}
