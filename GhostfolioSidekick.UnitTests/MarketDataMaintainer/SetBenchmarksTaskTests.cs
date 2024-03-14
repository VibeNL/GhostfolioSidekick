using AutoFixture;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.MarketDataMaintainer;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.UnitTests.MarketDataMaintainer
{
	public class SetBenchmarksTaskTests
	{
		private readonly Mock<ILogger<SetBenchmarksTask>> loggerMock;
		private readonly Mock<IMarketDataService> marketDataServiceMock;
		private readonly Mock<IApplicationSettings> applicationSettingsMock;
		private readonly SetBenchmarksTask setBenchmarksTask;

		public SetBenchmarksTaskTests()
		{
			loggerMock = new Mock<ILogger<SetBenchmarksTask>>();
			marketDataServiceMock = new Mock<IMarketDataService>();
			applicationSettingsMock = new Mock<IApplicationSettings>();

			setBenchmarksTask = new SetBenchmarksTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				applicationSettingsMock.Object);
		}

		[Fact]
		public async Task DoWork_Unauthorized_ShouldSetAllowAdminCallsToFalse()
		{
			// Arrange
			var task = new SetBenchmarksTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				applicationSettingsMock.Object);

			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Benchmarks = [new Fixture().Create<SymbolConfiguration>()]
			});

			marketDataServiceMock.Setup(x => x.FindSymbolByIdentifier(
				It.IsAny<string[]>(),
				It.IsAny<Currency?>(),
				It.IsAny<AssetClass[]>(),
				It.IsAny<AssetSubClass[]>(),
				It.IsAny<bool>(),
				It.IsAny<bool>())).ThrowsAsync(new NotAuthorizedException());

			// Act
			await task.DoWork();

			// Assert
			applicationSettingsMock.VerifySet(x => x.AllowAdminCalls = false, Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldSetBenchmarks()
		{
			// Arrange
			var benchmark = new Fixture().Create<SymbolConfiguration>();
			var profile = new Fixture().Create<SymbolProfile>();
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Benchmarks = [benchmark]
			});

			marketDataServiceMock.Setup(x => x.FindSymbolByIdentifier(
				It.IsAny<string[]>(),
				It.IsAny<Currency?>(),
				It.IsAny<AssetClass[]>(),
				It.IsAny<AssetSubClass[]>(),
				It.IsAny<bool>(),
				It.IsAny<bool>())).ReturnsAsync(profile);

			// Act
			await setBenchmarksTask.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.SetSymbolAsBenchmark(profile), Times.Once);
		}
	}
}
