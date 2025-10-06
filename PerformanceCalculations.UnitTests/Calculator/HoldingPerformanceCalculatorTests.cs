using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PerformanceCalculations.Calculator;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GhostfolioSidekick.PerformanceCalculations.UnitTests.Calculator
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "Its a test")]
	public class HoldingPerformanceCalculatorTests : IDisposable
	{
		private readonly DbContextOptions<DatabaseContext> _dbContextOptions;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly string _databaseFilePath;

		public HoldingPerformanceCalculatorTests()
		{
			_databaseFilePath = $"test_holdingperf_{Guid.NewGuid()}.db";
			_dbContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite($"Data Source={_databaseFilePath}")
				.Options;

			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_mockCurrencyExchange
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldReturnEmptyList_WhenNoHoldingsExist()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldSkipHoldings_WhenNoSymbolProfilesExist()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var holding = CreateHolding(symbolProfiles: [], activities: []);
			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldSkipHolding_WhenSymbolProfileIsNull()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var holding = new Holding
			{
				SymbolProfiles = [],
				Activities = []
			};
			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldReturnCorrectHoldingAggregated_WhenValidDataExists()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");
			var activities = new[]
			{
				CreateBuyActivity(account, DateTime.Today.AddDays(-10), 100, new Money(Currency.USD, 150), "T1")
			};
			var holding = CreateHolding([symbolProfile], activities);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			result.Should().HaveCount(1);
			var holdingAggregated = result.First();
			holdingAggregated.Symbol.Should().StartWith("AAPL_"); // Account for unique suffix
			holdingAggregated.Name.Should().Be("Apple Inc.");
			holdingAggregated.DataSource.Should().Be(Datasource.YAHOO);
			holdingAggregated.ActivityCount.Should().Be(1);
			holdingAggregated.AssetClass.Should().Be(AssetClass.Equity);
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldHandleEmptyActivities_ReturnEmptyCalculatedSnapshots()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var holding = CreateHolding([symbolProfile], []);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			result.Should().HaveCount(1);
			var holdingAggregated = result.First();
			holdingAggregated.CalculatedSnapshots.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldCalculateCorrectSnapshots_WithSingleBuyActivity()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");
			var buyDate = DateTime.Today.AddDays(-5);
			var activities = new[]
			{
				CreateBuyActivity(account, buyDate, 100, new Money(Currency.USD, 150), "T1")
			};
			var holding = CreateHolding([symbolProfile], activities);

			// Add market data
			var marketData = new MarketData
			{
				Date = DateOnly.FromDateTime(buyDate),
				Close = new Money(Currency.USD, 155),
				Open = new Money(Currency.USD, 150),
				High = new Money(Currency.USD, 160),
				Low = new Money(Currency.USD, 145),
				TradingVolume = 1000000
			};
			symbolProfile.MarketData.Add(marketData);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();
			// Calculator creates snapshots from buy date to today (6 days including today)
			var expectedSnapshotCount = (DateOnly.FromDateTime(DateTime.Today).DayNumber - DateOnly.FromDateTime(buyDate).DayNumber) + 1;
			holdingAggregated.CalculatedSnapshots.Should().HaveCount(expectedSnapshotCount);

			var firstSnapshot = holdingAggregated.CalculatedSnapshots.First();
			firstSnapshot.Date.Should().Be(DateOnly.FromDateTime(buyDate));
			firstSnapshot.Quantity.Should().Be(100);
			firstSnapshot.TotalValue.Amount.Should().Be(155 * 100); // Market price * quantity
			firstSnapshot.TotalInvested.Amount.Should().Be(150 * 100); // Unit price * quantity (TotalTransactionAmount)
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldHandleMultipleBuyActivities_CalculateCorrectAverageCostPrice()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			var firstBuyDate = DateTime.Today.AddDays(-10);
			var secondBuyDate = DateTime.Today.AddDays(-5);

			var activities = new[]
			{
				CreateBuyActivity(account, firstBuyDate, 100, new Money(Currency.USD, 150), "T1"),
				CreateBuyActivity(account, secondBuyDate, 50, new Money(Currency.USD, 160), "T2")
			};
			var holding = CreateHolding([symbolProfile], activities);

			// Add market data for both dates and today
			var marketData1 = new MarketData
			{
				Date = DateOnly.FromDateTime(firstBuyDate),
				Close = new Money(Currency.USD, 155),
				Open = new Money(Currency.USD, 150),
				High = new Money(Currency.USD, 160),
				Low = new Money(Currency.USD, 145),
				TradingVolume = 1000000
			};
			var marketData2 = new MarketData
			{
				Date = DateOnly.FromDateTime(secondBuyDate),
				Close = new Money(Currency.USD, 165),
				Open = new Money(Currency.USD, 160),
				High = new Money(Currency.USD, 170),
				Low = new Money(Currency.USD, 155),
				TradingVolume = 800000
			};
			// Add market data for today to get the correct final value
			var marketDataToday = new MarketData
			{
				Date = DateOnly.FromDateTime(DateTime.Today),
				Close = new Money(Currency.USD, 155), // Use a different price for today
				Open = new Money(Currency.USD, 160),
				High = new Money(Currency.USD, 170),
				Low = new Money(Currency.USD, 150),
				TradingVolume = 900000
			};
			symbolProfile.MarketData.Add(marketData1);
			symbolProfile.MarketData.Add(marketData2);
			symbolProfile.MarketData.Add(marketDataToday);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();
			// Calculator creates snapshots from first buy date to today 
			var expectedSnapshotCount = (DateOnly.FromDateTime(DateTime.Today).DayNumber - DateOnly.FromDateTime(firstBuyDate).DayNumber) + 1;
			holdingAggregated.CalculatedSnapshots.Should().HaveCount(expectedSnapshotCount);

			var finalSnapshot = holdingAggregated.CalculatedSnapshots.Last();
			finalSnapshot.Quantity.Should().Be(150); // Total quantity
			finalSnapshot.TotalValue.Amount.Should().Be(155 * 150); // Today's market price * total quantity
			finalSnapshot.TotalInvested.Amount.Should().Be((100 * 150) + (50 * 160)); // First buy: 100*150 + Second buy: 50*160 = 23000
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldHandleSellActivities_ReduceQuantityAndTotalInvested()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			var buyDate = DateTime.Today.AddDays(-10);
			var sellDate = DateTime.Today.AddDays(-5);

			var activities = new Activity[]
			{
				CreateBuyActivity(account, buyDate, 100, new Money(Currency.USD, 150), "T1"),
				CreateSellActivity(account, sellDate, 30, new Money(Currency.USD, 160), "T2") // Sell 30 shares
			};
			var holding = CreateHolding([symbolProfile], activities);

			// Add market data
			var marketData1 = new MarketData
			{
				Date = DateOnly.FromDateTime(buyDate),
				Close = new Money(Currency.USD, 155),
				Open = new Money(Currency.USD, 150),
				High = new Money(Currency.USD, 160),
				Low = new Money(Currency.USD, 145),
				TradingVolume = 1000000
			};
			var marketData2 = new MarketData
			{
				Date = DateOnly.FromDateTime(sellDate),
				Close = new Money(Currency.USD, 165),
				Open = new Money(Currency.USD, 160),
				High = new Money(Currency.USD, 170),
				Low = new Money(Currency.USD, 155),
				TradingVolume = 800000
			};
			symbolProfile.MarketData.Add(marketData1);
			symbolProfile.MarketData.Add(marketData2);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();
			var finalSnapshot = holdingAggregated.CalculatedSnapshots.Last();
			finalSnapshot.Quantity.Should().Be(70); // 100 - 30
													// TotalInvested: buy 100*150 = 15000, sell reduces by cost basis of 30*150 = 4500, total = 10500
			finalSnapshot.TotalInvested.Amount.Should().Be(10500);
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldUseLastKnownMarketPrice_WhenMarketDataMissing()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			var buyDate = DateTime.Today.AddDays(-5);
			var activities = new[]
			{
				CreateBuyActivity(account, buyDate, 100, new Money(Currency.USD, 150), "T1")
			};
			var holding = CreateHolding([symbolProfile], activities);

			// Add market data for a date before the buy date
			var marketData = new MarketData
			{
				Date = DateOnly.FromDateTime(buyDate.AddDays(-2)),
				Close = new Money(Currency.USD, 145),
				Open = new Money(Currency.USD, 140),
				High = new Money(Currency.USD, 150),
				Low = new Money(Currency.USD, 135),
				TradingVolume = 1000000
			};
			symbolProfile.MarketData.Add(marketData);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();
			var firstSnapshot = holdingAggregated.CalculatedSnapshots.First();
			firstSnapshot.TotalValue.Amount.Should().Be(145 * 100); // Should use last known price
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldHandleCurrencyConversion()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.EUR); // Use EUR as symbol currency
			var account = CreateAccount("Test Account");
			var activities = new[]
			{
				CreateBuyActivity(account, DateTime.Today, 100, new Money(Currency.USD, 150), "T1") // Activity in USD
			};
			var holding = CreateHolding([symbolProfile], activities);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			// Setup currency conversion from USD to EUR (activity currency to symbol currency)
			_mockCurrencyExchange
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), Currency.EUR, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency targetCurrency, DateOnly date) =>
					new Money(targetCurrency, money.Amount * 0.85m)); // USD to EUR conversion

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			result.Should().HaveCount(1);
			_mockCurrencyExchange.Verify(
				x => x.ConvertMoney(It.IsAny<Money>(), Currency.EUR, It.IsAny<DateOnly>()),
				Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldHandleZeroDivision_WhenQuantityIsZero()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			// Create activities that result in zero quantity (buy and sell same amount)
			var buyDate = DateTime.Today.AddDays(-5);
			var sellDate = DateTime.Today.AddDays(-3);

			var activities = new Activity[]
			{
				CreateBuyActivity(account, buyDate, 100, new Money(Currency.USD, 150), "T1"),
				CreateSellActivity(account, sellDate, -100, new Money(Currency.USD, 160), "T2")
			};
			var holding = CreateHolding([symbolProfile], activities);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act & Assert - Should not throw exception
			var result = await calculator.GetCalculatedHoldings();
			result.Should().HaveCount(1);
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldHandleNegativeQuantity_FromOverselling()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			var buyDate = DateTime.Today.AddDays(-5);
			var sellDate = DateTime.Today.AddDays(-3);

			// Sell more than owned (potential bug scenario)
			var activities = new Activity[]
			{
				CreateBuyActivity(account, buyDate, 100, new Money(Currency.USD, 150), "T1"),
				CreateSellActivity(account, sellDate, 150, new Money(Currency.USD, 160), "T2") // Oversell
			};
			var holding = CreateHolding([symbolProfile], activities);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();
			var finalSnapshot = holdingAggregated.CalculatedSnapshots.Last();
			finalSnapshot.Quantity.Should().Be(-50); // This could be a bug - negative quantities
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldHandleMultipleSymbolProfiles_UseFirstOne()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile1 = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var symbolProfile2 = CreateSymbolProfile("AAPL", "Apple Inc. (Alternative)", Currency.USD); // Use same base symbol
			var account = CreateAccount("Test Account");
			var activities = new[]
			{
				CreateBuyActivity(account, DateTime.Today, 100, new Money(Currency.USD, 150), "T1")
			};
			var holding = CreateHolding([symbolProfile1, symbolProfile2], activities);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();
			holdingAggregated.Symbol.Should().StartWith("AAPL_"); // Should use first symbol profile (with unique suffix)
			holdingAggregated.Name.Should().Be("Apple Inc."); // Should use first symbol profile
		}

		[Fact]
		public async Task GetCalculatedHoldings_ShouldIgnoreNonBuySellActivities()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			// Create a dividend activity (not BuySellActivity)
			var dividendActivity = new DividendActivity(
				account,
				null,
				[],
				DateTime.Today.AddDays(-5),
				new Money(Currency.USD, 10),
				"DIV1",
				null,
				"Dividend payment"
			);

			var activities = new Activity[]
			{
				CreateBuyActivity(account, DateTime.Today.AddDays(-5), 100, new Money(Currency.USD, 150), "T1"),
				dividendActivity
			};
			var holding = CreateHolding([symbolProfile], activities);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();
			holdingAggregated.ActivityCount.Should().Be(2); // Total activities
															// Calculator creates snapshots from buy date to today (6 days including today)
			var expectedSnapshotCount = (DateOnly.FromDateTime(DateTime.Today).DayNumber - DateOnly.FromDateTime(DateTime.Today.AddDays(-5)).DayNumber) + 1;
			holdingAggregated.CalculatedSnapshots.Should().HaveCount(expectedSnapshotCount);
		}

		[Fact]
		public async Task CalculateSnapShots_ShouldHandleAverageCostPriceCalculation_PotentialDivisionByZeroBug()
		{
			// Arrange - This test specifically targets a potential bug in the average cost price calculation
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			var activities = new[]
			{
				// Sell activity before any buy (edge case that could cause division by zero)
				CreateSellActivity(account, DateTime.Today.AddDays(-5), 50, new Money(Currency.USD, 160), "T1")
			};
			var holding = CreateHolding([symbolProfile], activities);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act & Assert - This should handle the edge case gracefully
			var result = await calculator.GetCalculatedHoldings();
			result.Should().HaveCount(1);

			var holdingAggregated = result.First();
			var firstSnapshot = holdingAggregated.CalculatedSnapshots.First();
			// The calculation should handle negative quantities without throwing
			firstSnapshot.Quantity.Should().Be(-50);
		}

		private DatabaseContext CreateDatabaseContext()
		{
			var context = new DatabaseContext(_dbContextOptions);
			context.Database.EnsureCreated();
			// Ensure clean state for each test
			context.ChangeTracker.Clear();
			return context;
		}

		private HoldingPerformanceCalculator CreateCalculator(DatabaseContext context)
		{
			return new HoldingPerformanceCalculator(context, _mockCurrencyExchange.Object);
		}

		private static Holding CreateHolding(IList<SymbolProfile> symbolProfiles, ICollection<Activity> activities)
		{
			return new Holding
			{
				SymbolProfiles = [.. symbolProfiles],
				Activities = [.. activities]
			};
		}

		private static int _symbolProfileCounter;
		private static int _accountCounter;

		private static SymbolProfile CreateSymbolProfile(string symbol, string name, Currency currency)
		{
			// Ensure unique symbol to avoid EF tracking conflicts
			var uniqueSymbol = $"{symbol}_{++_symbolProfileCounter}";
			return new SymbolProfile(
				uniqueSymbol,
				name,
				[uniqueSymbol],
				currency,
				Datasource.YAHOO,
				AssetClass.Equity,
				AssetSubClass.Stock,
				[],
				[])
			{
				MarketData = []
			};
		}

		private static Account CreateAccount(string name)
		{
			// Ensure unique account name to avoid EF tracking conflicts
			var uniqueName = $"{name}_{++_accountCounter}";
			return new Account(uniqueName)
			{
				Balance = []
			};
		}

		private static BuyActivity CreateBuyActivity(Account account, DateTime date, decimal quantity, Money unitPrice, string transactionId)
		{
			var activity = new BuyActivity(
				account,
				null,
				[],
				date,
				quantity,
				unitPrice,
				transactionId,
				null,
				null)
			{
				AdjustedQuantity = quantity,
				AdjustedUnitPrice = unitPrice
			};

			// Set TotalTransactionAmount to quantity * unitPrice (for testing purposes)
			activity.TotalTransactionAmount = new Money(unitPrice.Currency, Math.Abs(quantity) * unitPrice.Amount);

			return activity;
		}

		private static SellActivity CreateSellActivity(Account account, DateTime date, decimal quantity, Money unitPrice, string transactionId)
		{
			var activity = new SellActivity(
				account,
				null,
				[],
				date,
				quantity,
				unitPrice,
				transactionId,
				null,
				null)
			{
				AdjustedQuantity = quantity,
				AdjustedUnitPrice = unitPrice
			};

			// Set TotalTransactionAmount to quantity * unitPrice (for testing purposes)
			activity.TotalTransactionAmount = new Money(unitPrice.Currency, Math.Abs(quantity) * unitPrice.Amount);

			return activity;
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

		[Fact]
		public async Task GetCalculatedHoldings_ShouldCalculateTotalInvested_WithBuyAndSellActivities()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var account = CreateAccount("Test Account");

			var buyDate = DateTime.Today.AddDays(-10);
			var sellDate = DateTime.Today.AddDays(-5);

			var activities = new Activity[]
			{
				CreateBuyActivity(account, buyDate, 100, new Money(Currency.USD, 150), "T1"),
				CreateSellActivity(account, sellDate, 30, new Money(Currency.USD, 160), "T2") // Sell 30 shares
			};
			var holding = CreateHolding([symbolProfile], activities);

			// Add market data
			var marketData1 = new MarketData
			{
				Date = DateOnly.FromDateTime(buyDate),
				Close = new Money(Currency.USD, 155),
				Open = new Money(Currency.USD, 150),
				High = new Money(Currency.USD, 160),
				Low = new Money(Currency.USD, 145),
				TradingVolume = 1000000
			};
			var marketData2 = new MarketData
			{
				Date = DateOnly.FromDateTime(sellDate),
				Close = new Money(Currency.USD, 165),
				Open = new Money(Currency.USD, 160),
				High = new Money(Currency.USD, 170),
				Low = new Money(Currency.USD, 155),
				TradingVolume = 800000
			};
			symbolProfile.MarketData.Add(marketData1);
			symbolProfile.MarketData.Add(marketData2);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync();

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedHoldings();

			// Assert
			var holdingAggregated = result.First();

			// Check first snapshot (buy day)
			var firstSnapshot = holdingAggregated.CalculatedSnapshots.First();
			firstSnapshot.Date.Should().Be(DateOnly.FromDateTime(buyDate));
			firstSnapshot.Quantity.Should().Be(100);
			firstSnapshot.TotalInvested.Amount.Should().Be(15000); // 100 * 150

			// Check sell day snapshot
			var sellSnapshot = holdingAggregated.CalculatedSnapshots
				.FirstOrDefault(s => s.Date == DateOnly.FromDateTime(sellDate));
			sellSnapshot.Should().NotBeNull();
			sellSnapshot!.Quantity.Should().Be(70); // 100 - 30
			sellSnapshot.TotalInvested.Amount.Should().Be(10500); // 15000 - (30 * 150) = 10500 (cost basis reduction using average cost)

			// Check final snapshot
			var finalSnapshot = holdingAggregated.CalculatedSnapshots.Last();
			finalSnapshot.Quantity.Should().Be(70); // 100 - 30
			finalSnapshot.TotalInvested.Amount.Should().Be(10500); // Should remain the same
		}
	}
}