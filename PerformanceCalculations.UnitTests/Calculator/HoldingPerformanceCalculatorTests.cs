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
	public class PerformanceCalculatorTests : IDisposable
	{
		private readonly DbContextOptions<DatabaseContext> _dbContextOptions;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly string _databaseFilePath;

		public PerformanceCalculatorTests()
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
		public async Task GetCalculatedSnapshots_ShouldReturnEmptyList_WhenNoSymbolProfilesExist()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var holding = CreateHolding(symbolProfiles: [], activities: []);
			context.Holdings.Add(holding);
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldReturnEmptyList_WhenSymbolProfileCurrencyIsNone()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.NONE);
			var account = CreateAccount("Test Account");
			var activities = new[]
			{
				CreateBuyActivity(account, DateTime.Today.AddDays(-10), 100, new Money(Currency.USD, 150), "T1")
			};
			var holding = CreateHolding([symbolProfile], activities);
			context.Holdings.Add(holding);
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldReturnCorrectSnapshots_WhenValidDataExists()
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
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			var snapshots = result.ToList();
			snapshots.Should().NotBeEmpty();
			var expectedSnapshotCount = (DateOnly.FromDateTime(DateTime.Today).DayNumber - DateOnly.FromDateTime(DateTime.Today.AddDays(-10)).DayNumber) + 1;
			snapshots.Should().HaveCount(expectedSnapshotCount);
			snapshots.First().AccountId.Should().Be(account.Id);
			snapshots.First().Currency.Should().Be(Currency.USD);
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldReturnEmptyList_WhenNoActivitiesExist()
		{
			// Arrange
			using var context = CreateDatabaseContext();
			var symbolProfile = CreateSymbolProfile("AAPL", "Apple Inc.", Currency.USD);
			var holding = CreateHolding([symbolProfile], []);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldCalculateCorrectSnapshots_WithSingleBuyActivity()
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
			var marketData = new MarketData(
				Currency.USD,
				155,
				150,
				160,
				145,
				1000000,
				DateOnly.FromDateTime(buyDate));
			symbolProfile.MarketData.Add(marketData);

			context.Holdings.Add(holding);
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			var snapshots = result.ToList();
			var expectedSnapshotCount = (DateOnly.FromDateTime(DateTime.Today).DayNumber - DateOnly.FromDateTime(buyDate).DayNumber) + 1;
			snapshots.Should().HaveCount(expectedSnapshotCount);

			var firstSnapshot = snapshots.First();
			firstSnapshot.Date.Should().Be(DateOnly.FromDateTime(buyDate));
			firstSnapshot.Quantity.Should().Be(100);
			firstSnapshot.TotalValue.Should().Be(155 * 100); // Market price * quantity
			firstSnapshot.TotalInvested.Should().Be(150 * 100); // Unit price * quantity (TotalTransactionAmount)
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldHandleMultipleBuyActivities_CalculateCorrectAverageCostPrice()
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
			symbolProfile.MarketData.Add(new MarketData(Currency.USD, 155, 150, 160, 145, 1000000, DateOnly.FromDateTime(firstBuyDate)));
			symbolProfile.MarketData.Add(new MarketData(Currency.USD, 165, 160, 170, 155, 800000, DateOnly.FromDateTime(secondBuyDate)));
			symbolProfile.MarketData.Add(new MarketData(Currency.USD, 155, 160, 170, 150, 900000, DateOnly.FromDateTime(DateTime.Today)));

			context.Holdings.Add(holding);
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			var snapshots = result.ToList();
			var expectedSnapshotCount = (DateOnly.FromDateTime(DateTime.Today).DayNumber - DateOnly.FromDateTime(firstBuyDate).DayNumber) + 1;
			snapshots.Should().HaveCount(expectedSnapshotCount);

			var finalSnapshot = snapshots.Last();
			finalSnapshot.Quantity.Should().Be(150); // Total quantity
			finalSnapshot.TotalValue.Should().Be(155 * 150); // Today's market price * total quantity
			finalSnapshot.TotalInvested.Should().Be((100 * 150) + (50 * 160)); // First buy: 100*150 + Second buy: 50*160 = 23000
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldHandleSellActivities_ReduceQuantityAndTotalInvested()
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
			symbolProfile.MarketData.Add(new MarketData(Currency.USD, 155, 150, 160, 145, 1000000, DateOnly.FromDateTime(buyDate)));
			symbolProfile.MarketData.Add(new MarketData(Currency.USD, 165, 160, 170, 155, 800000, DateOnly.FromDateTime(sellDate)));

			context.Holdings.Add(holding);
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			var snapshots = result.ToList();
			var finalSnapshot = snapshots.Last();
			finalSnapshot.Quantity.Should().Be(70); // 100 - 30
			// TotalInvested: buy 100*150 = 15000, sell reduces by cost basis of 30*150 = 4500, total = 10500
			finalSnapshot.TotalInvested.Should().Be(10500);
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldUseLastKnownMarketPrice_WhenMarketDataMissing()
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
			symbolProfile.MarketData.Add(new MarketData(Currency.USD, 145, 140, 150, 135, 1000000, DateOnly.FromDateTime(buyDate.AddDays(-2))));

			context.Holdings.Add(holding);
			await context.SaveChangesAsync(CancellationToken.None);

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.USD);

			// Assert
			var snapshots = result.ToList();
			var firstSnapshot = snapshots.First();
			firstSnapshot.TotalValue.Should().Be(145 * 100); // Should use last known price
		}

		[Fact]
		public async Task GetCalculatedSnapshots_ShouldHandleCurrencyConversion()
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
			await context.SaveChangesAsync(CancellationToken.None);

			// Setup currency conversion from USD to EUR (activity currency to symbol currency)
			_mockCurrencyExchange
				.Setup(x => x.ConvertMoney(It.IsAny<Money>(), Currency.EUR, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency targetCurrency, DateOnly date) =>
					new Money(targetCurrency, money.Amount * 0.85m)); // USD to EUR conversion

			var calculator = CreateCalculator(context);

			// Act
			var result = await calculator.GetCalculatedSnapshots(holding, Currency.EUR);

			// Assert
			var snapshots = result.ToList();
			snapshots.Should().NotBeEmpty();
			_mockCurrencyExchange.Verify(
				x => x.ConvertMoney(It.IsAny<Money>(), Currency.EUR, It.IsAny<DateOnly>()),
				Times.AtLeastOnce);
		}

		private DatabaseContext CreateDatabaseContext()
		{
			var context = new DatabaseContext(_dbContextOptions);
			context.Database.EnsureCreated();
			context.ChangeTracker.Clear();
			return context;
		}

       private PerformanceCalculator CreateCalculator(DatabaseContext context)
       {
           var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
           dbFactoryMock
               .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(context);
           return new PerformanceCalculator(dbFactoryMock.Object, _mockCurrencyExchange.Object);
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
				AdjustedUnitPrice = unitPrice,
				TotalTransactionAmount = new Money(unitPrice.Currency, Math.Abs(quantity) * unitPrice.Amount)
			};

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
				AdjustedUnitPrice = unitPrice,
				TotalTransactionAmount = new Money(unitPrice.Currency, Math.Abs(quantity) * unitPrice.Amount)
			};

			return activity;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);

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
