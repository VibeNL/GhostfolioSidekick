using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Tests.Services
{
	public class HoldingsDataServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDatabaseContext;
		private readonly Mock<IServerConfigurationService> _mockServerConfigurationService;
		private readonly Mock<ILogger<HoldingsDataService>> _mockLogger;
		private readonly HoldingsDataService _holdingsDataService;

		public HoldingsDataServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			_mockServerConfigurationService = new Mock<IServerConfigurationService>();
			_mockLogger = new Mock<ILogger<HoldingsDataService>>();

			// Setup default primary currency
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.USD);

			// The actual constructor signature from the source file
			_holdingsDataService = new HoldingsDataService(
				_mockDatabaseContext.Object,
				_mockServerConfigurationService.Object,
				_mockLogger.Object);
		}

		[Fact]
		public async Task GetHoldingsAsync_ShouldReturnHoldingsInPrimaryCurrency()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;

			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshotPrimaryCurrency(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshotsPrimaryCurrency = new List<CalculatedSnapshotPrimaryCurrency> { calculatedSnapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(cancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("AAPL");
			result[0].Name.Should().Be("Apple Inc");
			result[0].Currency.Should().Be(Currency.USD.Symbol);
		}

		private static HoldingAggregated CreateTestHoldingAggregated(string symbol, string? name)
		{
			return new HoldingAggregated
			{
				Symbol = symbol,
				Name = name,
				AssetClass = AssetClass.Equity,
				SectorWeights = new List<SectorWeight>(),
				CalculatedSnapshots = new List<CalculatedSnapshot>(),
				CalculatedSnapshotsPrimaryCurrency = new List<CalculatedSnapshotPrimaryCurrency>()
			};
		}

		private static CalculatedSnapshotPrimaryCurrency CreateTestCalculatedSnapshotPrimaryCurrency(HoldingAggregated holding, int? accountId, DateOnly? date)
		{
			return new CalculatedSnapshotPrimaryCurrency
			{
				Id = 1,
				AccountId = accountId ?? 1,
				Date = date ?? DateOnly.FromDateTime(DateTime.Now),
				Quantity = 10,
				AverageCostPrice = 100,
				CurrentUnitPrice = 110,
				TotalInvested = 1000,
				TotalValue = 1100
			};
		}
	}
}