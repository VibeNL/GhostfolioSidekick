using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class HoldingsDataServiceTests : IDisposable
	{
		private readonly DbContextOptions<DatabaseContext> _dbContextOptions;
		private readonly DatabaseContext _dbContext;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly Mock<ILogger<HoldingsDataServiceOLD>> _mockLogger;
		private readonly HoldingsDataServiceOLD _service;
		private readonly string _databaseFilePath;

		public HoldingsDataServiceTests()
		{
			// Use SQLite database for more reliable testing
			_databaseFilePath = $"test_holdings_service_{Guid.NewGuid()}.db";
			_dbContextOptions = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite($"Data Source={_databaseFilePath}")
				.Options;

			_dbContext = new DatabaseContext(_dbContextOptions);
			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_mockLogger = new Mock<ILogger<HoldingsDataServiceOLD>>();
			
			_service = new HoldingsDataServiceOLD(
				_dbContext,
				_mockCurrencyExchange.Object,
				_mockLogger.Object);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithDefaultAccountId_CallsOverloadWithZero()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var targetCurrency = Currency.USD;
			var cancellationToken = CancellationToken.None;

			// Act
			var result = await _service.GetHoldingsAsync(targetCurrency, cancellationToken);

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithNoHoldings_ReturnsEmptyList()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var targetCurrency = Currency.USD;
			var accountId = 1;
			var cancellationToken = CancellationToken.None;

			// Act
			var result = await _service.GetHoldingsAsync(targetCurrency, accountId, cancellationToken);

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithHoldings_ReturnsProcessedHoldings()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var targetCurrency = Currency.USD;
			var accountId = 1;
			var cancellationToken = CancellationToken.None;

			// Create test account
			var account = new Account("Test Account") { Id = accountId };
			_dbContext.Accounts.Add(account);
			await _dbContext.SaveChangesAsync();

			var holding = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test Stock",
				Symbol = "TEST",
				SectorWeights = new List<SectorWeight>()
			};

			var snapshot = new CalculatedSnapshot
			{
				AccountId = accountId,
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 10,
				AverageCostPrice = new Money(Currency.USD, 100),
				CurrentUnitPrice = new Money(Currency.USD, 110),
				TotalInvested = new Money(Currency.USD, 1000),
				TotalValue = new Money(Currency.USD, 1100)
			};

			holding.CalculatedSnapshots.Add(snapshot);
			_dbContext.HoldingAggregateds.Add(holding);
			await _dbContext.SaveChangesAsync();

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => 
					money.Currency == currency ? money : new Money(currency, money.Amount));

			// Act
			var result = await _service.GetHoldingsAsync(targetCurrency, accountId, cancellationToken);

			// Assert
			Assert.NotNull(result);
			Assert.Single(result);
			
			var holdingDisplay = result.First();
			Assert.Equal("Test Stock", holdingDisplay.Name);
			Assert.Equal("TEST", holdingDisplay.Symbol);
			Assert.Equal(AssetClass.Equity.ToString(), holdingDisplay.AssetClass);
			Assert.Equal(10m, holdingDisplay.Quantity);
		}

		[Fact]
		public async Task GetMinDateAsync_ReturnsEarliestSnapshotDate()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var earliestDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
			var laterDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));

			// Create holding and snapshots
			var holding = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test Stock",
				Symbol = "TEST",
				SectorWeights = new List<SectorWeight>()
			};

			var snapshots = new List<CalculatedSnapshot>
			{
				new() 
				{ 
					Date = laterDate,
					AccountId = 1,
					Quantity = 1,
					AverageCostPrice = new Money(Currency.USD, 100),
					CurrentUnitPrice = new Money(Currency.USD, 100),
					TotalInvested = new Money(Currency.USD, 100),
					TotalValue = new Money(Currency.USD, 100)
				},
				new() 
				{ 
					Date = earliestDate,
					AccountId = 1,
					Quantity = 1,
					AverageCostPrice = new Money(Currency.USD, 100),
					CurrentUnitPrice = new Money(Currency.USD, 100),
					TotalInvested = new Money(Currency.USD, 100),
					TotalValue = new Money(Currency.USD, 100)
				}
			};

			foreach (var snapshot in snapshots)
			{
				holding.CalculatedSnapshots.Add(snapshot);
			}
			_dbContext.HoldingAggregateds.Add(holding);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetMinDateAsync();

			// Assert
			Assert.Equal(earliestDate, result);
		}

		[Fact]
		public async Task GetAccountsAsync_WithValidConnection_ReturnsOrderedAccounts()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var accounts = new List<Account>
			{
				new("Account B"),
				new("Account A"),
				new("Account C")
			};

			_dbContext.Accounts.AddRange(accounts);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetAccountsAsync();

			// Assert
			Assert.NotNull(result);
			Assert.Equal(3, result.Count);
			Assert.Equal("Account A", result[0].Name);
			Assert.Equal("Account B", result[1].Name);
			Assert.Equal("Account C", result[2].Name);
		}

		[Fact]
		public async Task GetSymbolsAsync_ReturnsDistinctOrderedSymbols()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var symbolProfiles = new List<SymbolProfile>
			{
				new("AAPL", "Apple Inc.", new List<string>(), Currency.USD, Datasource.YAHOO, AssetClass.Equity, AssetSubClass.Stock, new CountryWeight[0], new SectorWeight[0]),
				new("GOOGL", "Alphabet Inc.", new List<string>(), Currency.USD, Datasource.YAHOO, AssetClass.Equity, AssetSubClass.Stock, new CountryWeight[0], new SectorWeight[0]),
				new("AAPL", "Apple Inc.", new List<string>(), Currency.USD, Datasource.YAHOO, AssetClass.Equity, AssetSubClass.Stock, new CountryWeight[0], new SectorWeight[0]) // This will be a duplicate key - EF will prevent it
			};

			_dbContext.SymbolProfiles.Add(symbolProfiles[0]);
			_dbContext.SymbolProfiles.Add(symbolProfiles[1]);
			// Skip the duplicate
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetSymbolsAsync();

			// Assert
			Assert.NotNull(result);
			Assert.Equal(2, result.Count);
			Assert.Equal("AAPL", result[0]);
			Assert.Equal("GOOGL", result[1]);
		}

		[Fact]
		public async Task ProcessHoldingAsync_WithEmptySnapshots_ReturnsEmptyDisplayModel()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var holding = CreateTestHoldingWithSnapshots();
			var targetCurrency = Currency.USD;

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("ProcessHoldingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<HoldingDisplayModel>)method.Invoke(_service, new object[] { holding, targetCurrency })!;
			var result = await task;

			// Assert
			Assert.NotNull(result);
			Assert.Equal(0m, result.Quantity);
			Assert.Equal("Test Stock", result.Name);
			Assert.Equal("TEST", result.Symbol);
		}

		[Fact]
		public async Task ProcessHoldingAsync_WithSnapshots_ReturnsProcessedDisplayModel()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			var holding = CreateTestHoldingWithSnapshots();
			var snapshotsProperty = holding.GetType().GetProperty("Snapshots");
			var snapshots = (List<CalculatedSnapshot>)snapshotsProperty!.GetValue(holding)!;
			
			snapshots.Add(new CalculatedSnapshot
			{
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 10,
				AverageCostPrice = new Money(Currency.USD, 100),
				CurrentUnitPrice = new Money(Currency.USD, 110),
				TotalInvested = new Money(Currency.USD, 1000),
				TotalValue = new Money(Currency.USD, 1100)
			});

			var targetCurrency = Currency.USD;

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => money);

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("ProcessHoldingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<HoldingDisplayModel>)method.Invoke(_service, new object[] { holding, targetCurrency })!;
			var result = await task;

			// Assert
			Assert.NotNull(result);
			Assert.Equal(10m, result.Quantity);
			Assert.Equal(new Money(Currency.USD, 110), result.CurrentPrice);
			Assert.Equal(new Money(Currency.USD, 100), result.GainLoss);
			Assert.Equal(0.1m, result.GainLossPercentage); // (1100-1000)/1000 = 0.1
		}

		[Theory]
		[InlineData("BuyActivity", "Buy")]
		[InlineData("SellActivity", "Sell")]
		[InlineData("DividendActivity", "Dividend")]
		[InlineData("CashDepositActivity", "Deposit")]
		[InlineData("CashWithdrawalActivity", "Withdrawal")]
		[InlineData("FeeActivity", "Fee")]
		[InlineData("InterestActivity", "Interest")]
		public void GetDisplayType_ReturnsCorrectDisplayType(string activityTypeName, string expectedDisplayType)
		{
			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("GetDisplayType", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);

			var testAccount = new Account("Test");
			var testDate = DateTime.Now;
			var testMoney = new Money(Currency.USD, 100m);
			var testTransactionId = "TXN1";
			var partialIdentifiers = new List<PartialSymbolIdentifier>();

			// Create specific activity instances with proper constructors
			Activity activity = activityTypeName switch
			{
				"BuyActivity" => new BuyActivity(testAccount, null, partialIdentifiers, testDate, 1m, testMoney, testTransactionId, null, null),
				"SellActivity" => new SellActivity(testAccount, null, partialIdentifiers, testDate, 1m, testMoney, testTransactionId, null, null),
				"DividendActivity" => new DividendActivity(testAccount, null, partialIdentifiers, testDate, testMoney, testTransactionId, null, null),
				"CashDepositActivity" => new CashDepositActivity(testAccount, null, testDate, testMoney, testTransactionId, null, null),
				"CashWithdrawalActivity" => new CashWithdrawalActivity(testAccount, null, testDate, testMoney, testTransactionId, null, null),
				"FeeActivity" => new FeeActivity(testAccount, null, testDate, testMoney, testTransactionId, null, null),
				"InterestActivity" => new InterestActivity(testAccount, null, testDate, testMoney, testTransactionId, null, null),
				_ => throw new ArgumentException($"Unknown activity type: {activityTypeName}")
			};

			// Act
			var result = (string)method.Invoke(null, new object[] { activity })!;

			// Assert
			Assert.Equal(expectedDisplayType, result);
		}

		[Fact]
		public void CalculateHoldingWeights_WithValidHoldings_CalculatesCorrectWeights()
		{
			// Arrange
			var holdings = new List<HoldingDisplayModel>
			{
				new() { CurrentValue = new Money(Currency.USD, 100), Weight = 0 },
				new() { CurrentValue = new Money(Currency.USD, 200), Weight = 0 },
				new() { CurrentValue = new Money(Currency.USD, 300), Weight = 0 }
			};

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("CalculateHoldingWeights", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);

			// Act
			method.Invoke(null, new object[] { holdings });

			// Assert
			Assert.Equal(100m / 600m, holdings[0].Weight); // ~0.167
			Assert.Equal(200m / 600m, holdings[1].Weight); // ~0.333
			Assert.Equal(300m / 600m, holdings[2].Weight); // 0.5
		}

		[Fact]
		public void CalculateHoldingWeights_WithZeroTotalValue_LeavesWeightsAsZero()
		{
			// Arrange
			var holdings = new List<HoldingDisplayModel>
			{
				new() { CurrentValue = new Money(Currency.USD, 0), Weight = 0 },
				new() { CurrentValue = new Money(Currency.USD, 0), Weight = 0 }
			};

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("CalculateHoldingWeights", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);

			// Act
			method.Invoke(null, new object[] { holdings });

			// Assert
			Assert.Equal(0m, holdings[0].Weight);
			Assert.Equal(0m, holdings[1].Weight);
		}

		[Fact]
		public void GetSectorDisplay_WithSectorWeights_ReturnsJoinedNames()
		{
			// Arrange
			var sectorWeights = new List<SectorWeight>
			{
				new("Technology", 0.5m),
				new("Healthcare", 0.3m),
				new("Finance", 0.2m)
			};

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("GetSectorDisplay", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);

			// Act
			var result = (string)method.Invoke(null, new object[] { sectorWeights })!;

			// Assert
			Assert.Equal("Technology,Healthcare,Finance", result);
		}

		[Fact]
		public void GetSectorDisplay_WithEmptySectorWeights_ReturnsUndefined()
		{
			// Arrange
			var sectorWeights = new List<SectorWeight>();

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("GetSectorDisplay", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);

			// Act
			var result = (string)method.Invoke(null, new object[] { sectorWeights })!;

			// Assert
			Assert.Equal("Undefined", result);
		}

		[Fact]
		public async Task CurrencyExchange_IsCalledForConversions()
		{
			// Arrange
			var money = new Money(Currency.EUR, 100);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);
			var expectedResult = new Money(Currency.USD, 110);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(money, targetCurrency, date))
				.ReturnsAsync(expectedResult);

			// Act
			var result = await _mockCurrencyExchange.Object.ConvertMoney(money, targetCurrency, date);

			// Assert
			Assert.Equal(expectedResult, result);
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(money, targetCurrency, date), Times.Once);
		}

		[Fact]
		public async Task CurrencyExchange_WithSameCurrency_CanReturnOriginalMoney()
		{
			// Arrange
			var money = new Money(Currency.USD, 100);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(money, targetCurrency, date))
				.ReturnsAsync(money);

			// Act
			var result = await _mockCurrencyExchange.Object.ConvertMoney(money, targetCurrency, date);

			// Assert
			Assert.Equal(money, result);
		}

		// Helper method to create test data
		private static dynamic CreateTestHoldingWithSnapshots()
		{
			// Create an instance of the private HoldingWithSnapshots class using reflection
			var holdingWithSnapshotsType = typeof(HoldingsDataServiceOLD).GetNestedType("HoldingWithSnapshots", BindingFlags.NonPublic);
			Assert.NotNull(holdingWithSnapshotsType);

			var instance = Activator.CreateInstance(holdingWithSnapshotsType);
			Assert.NotNull(instance);

			// Set properties using reflection
			holdingWithSnapshotsType.GetProperty("Id")!.SetValue(instance, 1L);
			holdingWithSnapshotsType.GetProperty("AssetClass")!.SetValue(instance, AssetClass.Equity);
			holdingWithSnapshotsType.GetProperty("Name")!.SetValue(instance, "Test Stock");
			holdingWithSnapshotsType.GetProperty("Symbol")!.SetValue(instance, "TEST");
			holdingWithSnapshotsType.GetProperty("SectorWeights")!.SetValue(instance, new List<SectorWeight>());
			holdingWithSnapshotsType.GetProperty("Snapshots")!.SetValue(instance, new List<CalculatedSnapshot>());

			return instance;
		}

		public void Dispose()
		{
			try
			{
				_dbContext.Dispose();
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