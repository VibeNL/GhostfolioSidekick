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
		public async Task ProcessActivities_ShouldNotSetMarketPrice_WhenNoActivitiesAreFound()
		{
			// Arrange
			var symbolConfiguration = new Fixture().Create<SymbolConfiguration>();
			var symbolProfile = new Fixture().Create<SymbolProfile>();
			var marketDataList = new Fixture().CreateMany<MarketData>().ToList();
			var unsortedActivities = new List<BuySellActivity>();
			var historicData = new Fixture().CreateMany<HistoricData>().ToList();

			// Act
			await manualPriceProcessor.ProcessActivities(symbolConfiguration, symbolProfile, marketDataList, unsortedActivities, historicData);

			// Assert
			marketDataServiceMock.Verify(x => x.SetMarketPrice(It.IsAny<SymbolProfile>(), It.IsAny<Money>(), It.IsAny<DateTime>()), Times.Never);
		}

		[Fact]
		public async Task ProcessActivities_ShouldSetMarketPrice_WhenPricesAreDifferent()
		{
			// Arrange
			var symbolConfiguration = new Fixture().Create<SymbolConfiguration>();
			var symbolProfile = new Fixture().Create<SymbolProfile>();
			var marketDataList = new Fixture().CreateMany<MarketData>().ToList();
			var unsortedActivities = new Fixture().CreateMany<BuySellActivity>().ToList();
			var historicData = new Fixture().CreateMany<HistoricData>().ToList();

			// Act
			await manualPriceProcessor.ProcessActivities(symbolConfiguration, symbolProfile, marketDataList, unsortedActivities, historicData);

			// Assert
			marketDataServiceMock.Verify(x => x.SetMarketPrice(It.IsAny<SymbolProfile>(), It.IsAny<Money>(), It.IsAny<DateTime>()), Times.AtLeastOnce);
		}
	}
}
