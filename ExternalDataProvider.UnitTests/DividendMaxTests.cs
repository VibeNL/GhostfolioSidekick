using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GhostfolioSidekick.ExternalDataProvider.DividendMax;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Moq;
using Moq.Protected;
using Xunit;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests
{
    public class DividendMaxTests
    {
        [Fact]
        public async Task Gather_ReturnsDividends_WhenDataIsValid()
        {
            // Arrange
            var symbol = new SymbolProfile(
                symbol: "AAPL",
                name: "Apple",
                identifiers: new List<string>(),
                currency: GhostfolioSidekick.Model.Currency.USD,
                dataSource: "Test",
                assetClass: GhostfolioSidekick.Model.Activities.AssetClass.Equity,
                assetSubClass: null,
                countries: [],
                sectors: []
            );
            var suggestJson = "[{\"path\":\"/apple-inc-dividends\"}]";
            var html = @"<table class='mdc-data-table__table'><tbody><tr><td></td><td></td><td></td><td>2099-12-31</td><td>2100-01-15</td><td>USD</td><td></td><td>1.23</td><td></td></tr></tbody></table>";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("suggest.json")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(suggestJson) });
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("apple-inc-dividends")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(html) });

            var httpClient = new HttpClient(handlerMock.Object);
            var repo = new DividendMaxScraper(httpClient);

            // Act
            var result = await repo.Gather(symbol);

            // Assert
            Assert.Single(result);
            var dividend = result[0];
            Assert.Equal("AAPL", dividend.Symbol);
            Assert.Equal(1.23m, dividend.Amount.Amount);
            Assert.Equal("USD", dividend.Amount.Currency.Symbol);
        }

        [Fact]
        public async Task Gather_ReturnsEmpty_WhenNoRows()
        {
            // Arrange
            var symbol = new SymbolProfile(
                symbol: "AAPL",
                name: "Apple",
                identifiers: new List<string>(),
                currency: GhostfolioSidekick.Model.Currency.USD,
                dataSource: "Test",
                assetClass: GhostfolioSidekick.Model.Activities.AssetClass.Equity,
                assetSubClass: null,
				countries: [],
                sectors: []
            );
            var suggestJson = "[{\"path\":\"/apple-inc-dividends\"}]";
            var html = @"<table class='mdc-data-table__table'><tbody></tbody></table>";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(suggestJson) });
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("apple-inc-dividends")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(html) });

            var httpClient = new HttpClient(handlerMock.Object);
            var repo = new DividendMaxScraper(httpClient);

            // Act
            var result = await repo.Gather(symbol);

            // Assert
            Assert.Empty(result);
        }
    }
}
