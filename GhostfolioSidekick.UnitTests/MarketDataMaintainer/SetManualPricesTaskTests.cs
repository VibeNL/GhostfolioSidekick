using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.MarketDataMaintainer.UnitTests
{
	public class SetManualPricesTaskTests
	{
		private readonly Mock<ILogger<CreateManualSymbolTask>> loggerMock;
		private readonly Mock<IMarketDataService> marketDataServiceMock;
		private readonly Mock<IActivitiesService> activitiesServiceMock;
		private readonly List<IFileImporter> importers;
		private readonly Mock<IApplicationSettings> applicationSettingsMock;
		private readonly SetManualPricesTask setManualPricesTask;

		public SetManualPricesTaskTests()
		{
			loggerMock = new Mock<ILogger<CreateManualSymbolTask>>();
			marketDataServiceMock = new Mock<IMarketDataService>();
			activitiesServiceMock = new Mock<IActivitiesService>();
			importers = new List<IFileImporter>();
			applicationSettingsMock = new Mock<IApplicationSettings>();

			setManualPricesTask = new SetManualPricesTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				activitiesServiceMock.Object,
				importers,
				applicationSettingsMock.Object);
		}

		[Fact]
		public void Priority_ShouldReturnSetManualPrices()
		{
			// Act
			var result = setManualPricesTask.Priority;

			// Assert
			result.Should().Be(TaskPriority.SetManualPrices);
		}

		[Fact]
		public void ExecutionFrequency_ShouldReturnOneHour()
		{
			// Act
			var result = setManualPricesTask.ExecutionFrequency;

			// Assert
			result.Should().Be(TimeSpan.FromHours(1));
		}

		[Fact]
		public async Task DoWork_ShouldCallExpectedMethods()
		{
			// Arrange
			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles()).ReturnsAsync(new List<SymbolProfile>());
			activitiesServiceMock.Setup(x => x.GetAllActivities()).ReturnsAsync(new List<Holding>());

			// Act
			await setManualPricesTask.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.GetAllSymbolProfiles(), Times.Once);
			activitiesServiceMock.Verify(x => x.GetAllActivities(), Times.Once);
		}

		// Add more tests for other methods and scenarios
	}
}
