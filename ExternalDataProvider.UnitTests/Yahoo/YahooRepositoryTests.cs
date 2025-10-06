using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Yahoo
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
			var currencyFrom = Currency.USD;
			var currencyTo = Currency.EUR;
			var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

			// Act
			var result = await _repository.GetCurrencyHistory(currencyFrom, currencyTo, fromDate);

			// Assert
			Assert.NotNull(result);
			Assert.IsType<IEnumerable<CurrencyExchangeRate>>(result, exactMatch: false);
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
			var symbol = new SymbolProfile("AAPL", "Apple Inc.", ["AAPL"], Currency.USD, "YAHOO", AssetClass.Equity, AssetSubClass.Stock, [], []);
			var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

			// Act
			var result = await _repository.GetStockMarketData(symbol, fromDate);

			// Assert
			Assert.NotNull(result);
			Assert.IsType<IEnumerable<MarketData>>(result, exactMatch: false);
		}

		[Fact]
		public async Task GetStockSplits_ShouldReturnStockSplits()
		{
			// Arrange
			var symbol = new SymbolProfile("AAPL", "Apple Inc.", ["AAPL"], Currency.USD, "YAHOO", AssetClass.Equity, AssetSubClass.Stock, [], []);
			var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

			// Act
			var result = await _repository.GetStockSplits(symbol, fromDate);

			// Assert
			Assert.NotNull(result);
			Assert.IsType<IEnumerable<StockSplit>>(result, exactMatch: false);
		}
	}
}
