using AutoFixture;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Parsers;
using Moq;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class ManualPriceProcessorTests
	{
		private readonly Mock<IMarketDataService> marketDataServiceMock;
		private readonly ManualPriceProcessor manualPriceProcessor;

		public ManualPriceProcessorTests()
		{
			marketDataServiceMock = new Mock<IMarketDataService>();
			manualPriceProcessor = new ManualPriceProcessor(marketDataServiceMock.Object);
		}

		[Fact]
		public async Task ProcessActivities_ShouldCallSetMarketPrice_WhenPriceDiffersFromExpected()
		{
			// Arrange
			var symbolConfiguration = new Fixture().Create<SymbolConfiguration>();
			var mdi = new Fixture().Create<SymbolProfile>();
			var md = new List<MarketData>();
			var unsortedActivities = new List<BuySellActivity>();
			var historicData = new List<HistoricData>();

			// Act
			await manualPriceProcessor.ProcessActivities(symbolConfiguration, mdi, md, unsortedActivities, historicData);

			// Assert
			marketDataServiceMock.Verify(x => x.SetMarketPrice(It.IsAny<SymbolProfile>(), It.IsAny<Money>(), It.IsAny<DateTime>()), Times.AtLeastOnce);
		}

		[Fact]
		public void ProcessActivities_ShouldNotCallSetMarketPrice_WhenPriceMatchesExpected()
		{
			// Arrange
			// Similar to the previous test, but you'll need to set up your mock data such that the expected price matches the actual price

			// Act
			// Similar to the previous test

			// Assert
			marketDataServiceMock.Verify(x => x.SetMarketPrice(It.IsAny<SymbolProfile>(), It.IsAny<Money>(), It.IsAny<DateTime>()), Times.Never);
		}

		// Add more tests here for other methods and scenarios
	}
}
