using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
	public class HoldingsDataServiceTests : IDisposable
	{
		private readonly DbContextOptions<DatabaseContext> _dbContextOptions;
		private readonly string _databaseFilePath;

		public HoldingsDataServiceTests()
		{
			_databaseFilePath = $"test_holdings_{Guid.NewGuid()}.db";
			_dbContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite($"Data Source={_databaseFilePath}")
				.Options;
		}

		[Fact]
		public async Task GetHoldingsAsync_ReturnsExpectedHoldings()
		{
			// Arrange
			var targetCurrency = Currency.USD;

			using var context = new DatabaseContext(_dbContextOptions);
			await context.Database.EnsureCreatedAsync();

			// Create test data
			var sectorWeight = new SectorWeight { Name = "Tech" };
			var holdingAggregated = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test Holding",
				Symbol = "TST",
				SectorWeights = new List<SectorWeight> { sectorWeight }
			};

			// Create calculated snapshot with the holding
			var calculatedSnapshot = new CalculatedSnapshot(
				0, // Id will be auto-generated
				0, // AccountId
				new DateOnly(2024, 1, 1),
				10,
				new Money(targetCurrency, 100),
				new Money(targetCurrency, 120),
				new Money(targetCurrency, 1000),
				new Money(targetCurrency, 1200)
			);

			holdingAggregated.CalculatedSnapshots.Add(calculatedSnapshot);

			context.HoldingAggregateds.Add(holdingAggregated);
			await context.SaveChangesAsync();

			var currencyExchangeMock = new Mock<ICurrencyExchange>();
			currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money m, Currency c, DateOnly d) => m);

			var loggerMock = new Mock<ILogger<HoldingsDataService>>();
			var service = new HoldingsDataService(context, currencyExchangeMock.Object, loggerMock.Object);

			// Act
			var result = await service.GetHoldingsAsync(targetCurrency);

			// Assert
			Assert.Single(result);
			var holding = result[0];
			Assert.Equal("TST", holding.Symbol);
			Assert.Equal("Test Holding", holding.Name);
			Assert.Equal(1200, holding.CurrentValue.Amount);
			Assert.Equal(10, holding.Quantity);
			Assert.Equal(100, holding.AveragePrice.Amount);
			Assert.Equal(120, holding.CurrentPrice.Amount);
			Assert.Equal(200, holding.GainLoss.Amount);
			Assert.Equal("Tech", holding.Sector);
			Assert.Equal("Equity", holding.AssetClass);
			Assert.Equal("USD", holding.Currency);
		}

		[Fact]
		public async Task GetHoldingsAsync_EmptySnapshots_ReturnsEmptySnapshot()
		{
			// Arrange
			var targetCurrency = Currency.USD;

			using var context = new DatabaseContext(_dbContextOptions);
			await context.Database.EnsureCreatedAsync();

			// Create holding without snapshots
			var holdingAggregated = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test Holding",
				Symbol = "TST",
				SectorWeights = new List<SectorWeight>()
			};

			context.HoldingAggregateds.Add(holdingAggregated);
			await context.SaveChangesAsync();

			var currencyExchangeMock = new Mock<ICurrencyExchange>();
			currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money m, Currency c, DateOnly d) => m);

			var loggerMock = new Mock<ILogger<HoldingsDataService>>();
			var service = new HoldingsDataService(context, currencyExchangeMock.Object, loggerMock.Object);

			// Act
			var result = await service.GetHoldingsAsync(targetCurrency);

			// Assert
			Assert.Single(result);
			var holding = result[0];
			Assert.Equal("TST", holding.Symbol);
			Assert.Equal(0, holding.CurrentValue.Amount);
			Assert.Equal(0, holding.Quantity);
		}

		[Fact]
		public async Task ConvertToTargetCurrency_ReturnsConvertedSnapshot()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var originalCurrency = Currency.EUR;
			var snapshot = new CalculatedSnapshot(
				1,
				0,
				new DateOnly(2024, 1, 1),
				10,
				new Money(originalCurrency, 100),
				new Money(originalCurrency, 120),
				new Money(originalCurrency, 1000),
				new Money(originalCurrency, 1200)
			);

			using var context = new DatabaseContext(_dbContextOptions);
			await context.Database.EnsureCreatedAsync();

			var currencyExchangeMock = new Mock<ICurrencyExchange>();
			currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money m, Currency c, DateOnly d) => new Money(targetCurrency, m.Amount * 2));

			var loggerMock = new Mock<ILogger<HoldingsDataService>>();
			var service = new HoldingsDataService(context, currencyExchangeMock.Object, loggerMock.Object);

			// Use reflection to call private method
			var method = typeof(HoldingsDataService).GetMethod("ConvertToTargetCurrency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
			var task = (Task<CalculatedSnapshot>)method.Invoke(service, new object[] { targetCurrency, snapshot })!;
			var result = await task;

			// Assert
			Assert.Equal(targetCurrency, result.AverageCostPrice.Currency);
			Assert.Equal(200, result.AverageCostPrice.Amount);
			Assert.Equal(240, result.CurrentUnitPrice.Amount);
			Assert.Equal(2000, result.TotalInvested.Amount);
			Assert.Equal(2400, result.TotalValue.Amount);
		}

		public void Dispose()
		{
			try
			{
				if (File.Exists(_databaseFilePath))
				{
					File.Delete(_databaseFilePath);
				}
			}
			catch (Exception)
			{
				// Ignore cleanup errors
			}
		}
	}
}