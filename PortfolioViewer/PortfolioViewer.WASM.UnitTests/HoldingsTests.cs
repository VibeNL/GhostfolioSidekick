using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

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
			var sectorWeight = new SectorWeight("Tech", 1.0m);
			var holdingAggregated = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test Holding",
				Symbol = "TST",
				SectorWeights = new List<SectorWeight> { sectorWeight }
			};

			// Create calculated snapshot with the holding
			var calculatedSnapshot = new CalculatedSnapshot
			{
				Id = 0, // Will be auto-generated
				AccountId = 0,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				AverageCostPrice = new Money(targetCurrency, 100),
				CurrentUnitPrice = new Money(targetCurrency, 120),
				TotalInvested = new Money(targetCurrency, 1000),
				TotalValue = new Money(targetCurrency, 1200)
			};

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
		public async Task ConvertSnapshotToTargetCurrency_ReturnsConvertedSnapshot()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var originalCurrency = Currency.EUR;
			var snapshot = new CalculatedSnapshot
			{
				Id = 1,
				AccountId = 0,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				AverageCostPrice = new Money(originalCurrency, 100),
				CurrentUnitPrice = new Money(originalCurrency, 120),
				TotalInvested = new Money(originalCurrency, 1000),
				TotalValue = new Money(originalCurrency, 1200)
			};

			using var context = new DatabaseContext(_dbContextOptions);
			await context.Database.EnsureCreatedAsync();

			var currencyExchangeMock = new Mock<ICurrencyExchange>();
			currencyExchangeMock.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money m, Currency c, DateOnly d) => new Money(targetCurrency, m.Amount * 2));

			var loggerMock = new Mock<ILogger<HoldingsDataService>>();
			var service = new HoldingsDataService(context, currencyExchangeMock.Object, loggerMock.Object);

			// Use reflection to call private method
			var method = typeof(HoldingsDataService).GetMethod("ConvertSnapshotToTargetCurrency", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method); // Ensure method exists
			
			var task = (Task<CalculatedSnapshot>)method.Invoke(service, new object[] { targetCurrency, snapshot })!;
			var result = await task;

			// Assert
			Assert.Equal(targetCurrency, result.AverageCostPrice.Currency);
			Assert.Equal(200, result.AverageCostPrice.Amount);
			Assert.Equal(240, result.CurrentUnitPrice.Amount);
			Assert.Equal(2000, result.TotalInvested.Amount);
			Assert.Equal(2400, result.TotalValue.Amount);
		}

		[Fact]
		public async Task GetMinDateAsync_ReturnsEarliestDate()
		{
			// Arrange
			using var context = new DatabaseContext(_dbContextOptions);
			await context.Database.EnsureCreatedAsync();

			var earliestDate = new DateOnly(2024, 1, 1);
			var laterDate = new DateOnly(2024, 1, 15);

			// Create a holding first to satisfy foreign key constraints
			var holding = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test Holding",
				Symbol = "TST",
				SectorWeights = new List<SectorWeight>()
			};

			context.HoldingAggregateds.Add(holding);
			await context.SaveChangesAsync();

			// Create test snapshots with reference to the holding
			var snapshot1 = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = laterDate,
				Quantity = 10,
				AverageCostPrice = new Money(Currency.USD, 100),
				CurrentUnitPrice = new Money(Currency.USD, 120),
				TotalInvested = new Money(Currency.USD, 1000),
				TotalValue = new Money(Currency.USD, 1200)
			};

			var snapshot2 = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = earliestDate,
				Quantity = 5,
				AverageCostPrice = new Money(Currency.USD, 90),
				CurrentUnitPrice = new Money(Currency.USD, 110),
				TotalInvested = new Money(Currency.USD, 450),
				TotalValue = new Money(Currency.USD, 550)
			};

			// Add snapshots to the holding
			holding.CalculatedSnapshots.Add(snapshot1);
			holding.CalculatedSnapshots.Add(snapshot2);
			await context.SaveChangesAsync();

			var currencyExchangeMock = new Mock<ICurrencyExchange>();
			var loggerMock = new Mock<ILogger<HoldingsDataService>>();
			var service = new HoldingsDataService(context, currencyExchangeMock.Object, loggerMock.Object);

			// Act
			var result = await service.GetMinDateAsync();

			// Assert
			Assert.Equal(earliestDate, result);
		}

		[Fact]
		public async Task GetAccountsAsync_ReturnsOrderedAccounts()
		{
			// Arrange
			using var context = new DatabaseContext(_dbContextOptions);
			await context.Database.EnsureCreatedAsync();

			var account1 = new Account("Zebra Account");
			var account2 = new Account("Alpha Account");
			var account3 = new Account("Beta Account");

			context.Accounts.AddRange(account1, account2, account3);
			await context.SaveChangesAsync();

			var currencyExchangeMock = new Mock<ICurrencyExchange>();
			var loggerMock = new Mock<ILogger<HoldingsDataService>>();
			var service = new HoldingsDataService(context, currencyExchangeMock.Object, loggerMock.Object);

			// Act
			var result = await service.GetAccountsAsync();

			// Assert
			Assert.Equal(3, result.Count);
			Assert.Equal("Alpha Account", result[0].Name);
			Assert.Equal("Beta Account", result[1].Name);
			Assert.Equal("Zebra Account", result[2].Name);
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