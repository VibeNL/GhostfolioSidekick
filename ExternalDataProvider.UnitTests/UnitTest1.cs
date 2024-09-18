using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.ExternalDataProvider.PolygonIO;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExternalDataProvider.UnitTests
{
	public class UnitTest1
	{
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
			var symbol = new SymbolProfile("AAPL", "Apple", [], Currency.USD, "", GhostfolioSidekick.Model.Activities.AssetClass.Undefined, null, [], []);
			var r = (await x.GetStockMarketData(symbol, DateOnly.FromDateTime(DateTime.Today.AddDays(-365 * 2)))).ToList();

			// Assert
			Assert.NotNull(r);
		}
	}
}