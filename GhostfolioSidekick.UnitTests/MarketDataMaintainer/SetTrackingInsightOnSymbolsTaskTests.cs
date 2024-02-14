using FluentAssertions;
using Moq;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.Extensions.Logging;
using GhostfolioSidekick.MarketDataMaintainer;
using AutoFixture;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class SetTrackingInsightOnSymbolsTaskTests
	{
		private readonly Mock<ILogger<SetTrackingInsightOnSymbolsTask>> loggerMock;
		private readonly Mock<IMarketDataService> marketDataServiceMock;
		private readonly Mock<IApplicationSettings> applicationSettingsMock;

		public SetTrackingInsightOnSymbolsTaskTests()
		{
			loggerMock = new Mock<ILogger<SetTrackingInsightOnSymbolsTask>>();
			marketDataServiceMock = new Mock<IMarketDataService>();
			applicationSettingsMock = new Mock<IApplicationSettings>();
		}

		[Fact]
		public async Task DoWork_Unauthorized_ShouldSetAllowAdminCallsToFalse()
		{
			// Arrange
			var task = new SetTrackingInsightOnSymbolsTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				applicationSettingsMock.Object);

			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles(It.IsAny<bool>())).ThrowsAsync(new NotAuthorizedException());

			// Act
			await task.DoWork();

			// Assert
			applicationSettingsMock.VerifySet(x => x.AllowAdminCalls = false, Times.Once);
		}

		[Fact]
		public async Task DoWork_SymbolNotFound_NotSet()
		{
			// Arrange
			var symbol = new Fixture().Create<SymbolProfile>();
			var config = new Fixture().Create<SymbolConfiguration>();
			var task = new SetTrackingInsightOnSymbolsTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				applicationSettingsMock.Object);

			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles(It.IsAny<bool>())).ReturnsAsync([symbol]);
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Symbols = [config]
			});

			// Act
			await task.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.UpdateSymbol(symbol), Times.Never);
			symbol.Mappings.TrackInsight.Should().NotBe(config.TrackingInsightSymbol);
		}

		[Fact]
		public async Task DoWork_NoSymbolsInTheConfig_ShouldCallSetTrackingInsightOnSymbols()
		{
			// Arrange
			var symbol = new Fixture().Create<SymbolProfile>();
			var task = new SetTrackingInsightOnSymbolsTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				applicationSettingsMock.Object);

			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles(It.IsAny<bool>())).ReturnsAsync([symbol]);
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Symbols = null
			});

			// Act
			await task.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.UpdateSymbol(symbol), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldCallSetTrackingInsightOnSymbols()
		{
			// Arrange
			var symbol = new Fixture().Create<SymbolProfile>();
			var config = new Fixture().Build<SymbolConfiguration>().With(x => x.Symbol, symbol.Symbol).Create();
			var task = new SetTrackingInsightOnSymbolsTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				applicationSettingsMock.Object);

			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles(It.IsAny<bool>())).ReturnsAsync([symbol]);
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Symbols = [config]
			});

			// Act
			await task.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.UpdateSymbol(symbol), Times.Once);
			symbol.Mappings.TrackInsight.Should().Be(config.TrackingInsightSymbol);
		}
	}
}
