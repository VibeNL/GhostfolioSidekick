using CoinGecko.Net.Clients;
using CoinGecko.Net.Interfaces;
using CoinGecko.Net.Objects.Models;
using CryptoExchange.Net.Objects;
using FluentAssertions;
using GhostfolioSidekick.ExternalDataProvider.CoinGecko;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.CoinGecko
{
	public class CoinGeckoRepositoryTests
	{
		private readonly Mock<ILogger<CoinGeckoRepository>> loggerMock;
		private readonly CoinGeckoRepository repository;
		private Mock<ICoinGeckoRestClient> restClientMock;

		public CoinGeckoRepositoryTests()
		{
			restClientMock = new Mock<ICoinGeckoRestClient>();
			loggerMock = new Mock<ILogger<CoinGeckoRepository>>();
			repository = new CoinGeckoRepository(loggerMock.Object, new MemoryCache(new MemoryCacheOptions()), restClientMock.Object);
		}

		[Fact]
		public void DataSource_ShouldReturn_Coingecko()
		{
			repository.DataSource.Should().Be(Datasource.COINGECKO);
		}

		[Fact]
		public void MinDate_ShouldReturn_OneYearAgo()
		{
			var expectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-365));
			repository.MinDate.Should().Be(expectedDate);
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturn_SymbolProfile_WhenIdentifierMatches()
		{
			// Arrange
			var identifiers = new[] { new PartialSymbolIdentifier { Identifier = "btc", AllowedAssetSubClasses = new List<AssetSubClass> { AssetSubClass.CryptoCurrency } } };
			var coinGeckoAsset = new CoinGeckoAsset { Id = "bitcoin", Name = "Bitcoin", Symbol = "btc" };
            restClientMock.Setup(c => c.Api.GetAssetsAsync(default, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoAsset>>(
                null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, new List<CoinGeckoAsset> { coinGeckoAsset }, null));

			// Act
			var result = await repository.MatchSymbol(identifiers);

			// Assert
            result.Should().NotBeNull();
            result.Symbol.Should().Be("btc");
            result.Name.Should().Be("Bitcoin");
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturn_Null_WhenNoIdentifierMatches()
		{
			// Arrange
			var identifiers = new[] { new PartialSymbolIdentifier { Identifier = "unknown", AllowedAssetSubClasses = new List<AssetSubClass> { AssetSubClass.CryptoCurrency } } };
			restClientMock.Setup(c => c.Api.GetAssetsAsync(default, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoAsset>>(
				null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, new List<CoinGeckoAsset> { }, null));

			// Act
			var result = await repository.MatchSymbol(identifiers);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task GetStockMarketData_ShouldReturn_MarketData_WhenSymbolMatches()
		{
			// Arrange
			var symbolProfile = new SymbolProfile("btc", "Bitcoin", new List<string> { "btc" }, Currency.USD, Datasource.COINGECKO, AssetClass.Liquidity, AssetSubClass.CryptoCurrency, new CountryWeight[0], new SectorWeight[0]);
			var coinGeckoAsset = new CoinGeckoAsset { Id = "bitcoin", Name = "Bitcoin", Symbol = "btc" };
			var ohlcData = new List<CoinGeckoOhlc> { new() { Timestamp = DateTime.UtcNow, Open = 100, Close = 200, High = 300, Low = 50 } };
            restClientMock.Setup(c => c.Api.GetOhlcAsync("bitcoin", "usd", 365, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoOhlc>>(
                null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, ohlcData, null));
            restClientMock.Setup(c => c.Api.GetOhlcAsync("bitcoin", "usd", 30, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoOhlc>>(
                null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, ohlcData, null));
			restClientMock.Setup(c => c.Api.GetAssetsAsync(default, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoAsset>>(
				null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, new List<CoinGeckoAsset> { coinGeckoAsset }, null));

			// Act
			var result = await repository.GetStockMarketData(symbolProfile, DateOnly.FromDateTime(DateTime.Today.AddDays(-365)));

			// Assert
			result.Should().NotBeEmpty();
			result.Should().ContainSingle();
		}

		[Fact]
		public async Task GetStockMarketData_ShouldReturn_Empty_WhenSymbolDoesNotMatch()
		{
			// Arrange
			var symbolProfile = new SymbolProfile("unknown", "Unknown", new List<string> { "unknown" }, Currency.USD, Datasource.COINGECKO, AssetClass.Liquidity, AssetSubClass.CryptoCurrency, new CountryWeight[0], new SectorWeight[0]);
            restClientMock.Setup(c => c.Api.GetOhlcAsync("unknown", "usd", 365, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoOhlc>>(
                null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, new List<CoinGeckoOhlc>(), null));
            restClientMock.Setup(c => c.Api.GetOhlcAsync("unknown", "usd", 30, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoOhlc>>(
                null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, new List<CoinGeckoOhlc>(), null));

			// Act
			var result = await repository.GetStockMarketData(symbolProfile, DateOnly.FromDateTime(DateTime.Today.AddDays(-365)));

			// Assert
			result.Should().BeEmpty();
		}

		//[Fact]
		//public async Task GetCoinGeckoAsset_ShouldReturn_Asset_WhenIdentifierMatches()
		//{
		//	// Arrange
		//	var identifier = "btc";
		//	var coinGeckoAsset = new CoinGeckoAsset { Id = "bitcoin", Name = "Bitcoin", Symbol = "btc" };
		//	var restClientMock = new Mock<ICoinGeckoRestClient>();
  //          restClientMock.Setup(c => c.Api.GetAssetsAsync(default, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoAsset>>(
  //              null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, new List<CoinGeckoAsset> { coinGeckoAsset }, null));

		//	// Act
		//	var result = await _repository.GetCoinGeckoAsset(identifier);

		//	// Assert
		//	Assert.NotNull(result);
		//	Assert.Equal("btc", result.Symbol);
		//	Assert.Equal("Bitcoin", result.Name);
		//}

		//[Fact]
		//public async Task GetCoinGeckoAsset_ShouldReturn_Null_WhenIdentifierDoesNotMatch()
		//{
		//	// Arrange
		//	var identifier = "unknown";
		//	var restClientMock = new Mock<ICoinGeckoRestClient>();
  //          restClientMock.Setup(c => c.Api.GetAssetsAsync(default, default)).ReturnsAsync(new WebCallResult<IEnumerable<CoinGeckoAsset>>(
  //              null, null, null, null, null, null, null, null, null, null, ResultDataSource.Cache, new List<CoinGeckoAsset>(), null));

		//	// Act
		//	var result = await _repository.GetCoinGeckoAsset(identifier);

		//	// Assert
		//	Assert.Null(result);
		//}
	}
}