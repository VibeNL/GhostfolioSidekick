using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.ExternalDataProvider.PolygonIO;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExternalDataProvider.UnitTests
{
	public class UnitTest1
	{
		private const string apiKey = "RE9XXn0JZkiD8DLjqePRk6zV7qF60cK1";

		[Fact]
		public async Task Test1()
		{
			// Arrange
			var appSettingsMock = new Mock<IApplicationSettings>();

			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPolygonIOApiKey = apiKey
				}
			});

			var x = new CurrencyRepository(new Mock<ILogger<CurrencyRepository>>().Object, appSettingsMock.Object);

			// Act
			var r = (await x.GetCurrencyHistory(Currency.EUR, Currency.USD, DateOnly.FromDateTime(DateTime.Today.AddDays(-365 * 2)))).ToList();

			// Assert
			Assert.NotNull(r);
		}

		[Fact]
		public async Task Test2()
		{
			// Arrange
			var appSettingsMock = new Mock<IApplicationSettings>();

			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPolygonIOApiKey = apiKey
				}
			});

			var x = new StockPriceRepository(new Mock<ILogger<StockPriceRepository>>().Object, appSettingsMock.Object);

			// Act
			var symbol = new SymbolProfile("AAPL", "Apple", [], Currency.USD, "", AssetClass.Undefined, null, [], []);
			var r = (await x.GetStockMarketData(symbol, DateOnly.FromDateTime(DateTime.Today.AddDays(-365 * 2)))).ToList();

			// Assert
			Assert.NotNull(r);
		}

		[Fact]
		public async Task Test3()
		{
			// Arrange
			var appSettingsMock = new Mock<IApplicationSettings>();

			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPolygonIOApiKey = apiKey
				}
			});

			var x = new StockPriceRepository(new Mock<ILogger<StockPriceRepository>>().Object, appSettingsMock.Object);

			// Act
			var symbol = new SymbolProfile("BTC", "Bitcoin", [], Currency.USD, "", AssetClass.Liquidity, AssetSubClass.CryptoCurrency, [], []);
			var r = (await x.GetStockMarketData(symbol, DateOnly.FromDateTime(DateTime.Today.AddDays(-365 * 2)))).ToList();

			// Assert
			Assert.NotNull(r);
		}

		[Fact]
		public async Task Test4()
		{
			// Arrange
			var appSettingsMock = new Mock<IApplicationSettings>();

			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPolygonIOApiKey = apiKey
				}
			});

			var x = new SymbolMatcher(new Mock<ILogger<SymbolMatcher>>().Object, appSettingsMock.Object);

			// Act
			var r = await x.MatchSymbol([PartialSymbolIdentifier.CreateGeneric("US2546871060")]);

			// Assert
			Assert.NotNull(r);
		}
	}
}