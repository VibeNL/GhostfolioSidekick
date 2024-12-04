using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using YahooFinanceApi;

namespace GhostfolioSidekick.Tests.ExternalDataProvider.Yahoo
{
	/// <summary>
	/// Warning: Uses the real Yahoo Finance API.
	/// </summary>
	public class YahooRepositoryTests
    {
        private readonly Mock<ILogger<YahooRepository>> _loggerMock;
        private readonly YahooRepository _repository;

        public YahooRepositoryTests()
        {
            _loggerMock = new Mock<ILogger<YahooRepository>>();
            _repository = new YahooRepository(_loggerMock.Object);
        }

        [Fact]
        public async Task GetCurrencyHistory_ShouldReturnMarketData()
        {
            // Arrange
            var currencyFrom = new Currency("USD");
            var currencyTo = new Currency("EUR");
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

            // Act
            var result = await _repository.GetCurrencyHistory(currencyFrom, currencyTo, fromDate);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<MarketData>>(result);
        }

        [Fact]
        public async Task MatchSymbol_ShouldReturnSymbolProfile()
        {
            // Arrange
            var identifiers = new[]
            {
                new PartialSymbolIdentifier { Identifier = "AAPL" }
            };

            // Act
            var result = await _repository.MatchSymbol(identifiers);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<SymbolProfile>(result);
        }

        [Fact]
        public async Task GetStockMarketData_ShouldReturnMarketData()
        {
            // Arrange
            var symbol = new SymbolProfile("AAPL", "Apple Inc.", new List<string> { "AAPL" }, new Currency("USD"), "YAHOO", AssetClass.Equity, AssetSubClass.Stock, new CountryWeight[0], new SectorWeight[0]);
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

            // Act
            var result = await _repository.GetStockMarketData(symbol, fromDate);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<MarketData>>(result);
        }

        [Fact]
        public async Task GetStockSplits_ShouldReturnStockSplits()
        {
            // Arrange
            var symbol = new SymbolProfile("AAPL", "Apple Inc.", new List<string> { "AAPL" }, new Currency("USD"), "YAHOO", AssetClass.Equity, AssetSubClass.Stock, new CountryWeight[0], new SectorWeight[0]);
            var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

            // Act
            var result = await _repository.GetStockSplits(symbol, fromDate);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<StockSplit>>(result);
        }
    }
}
