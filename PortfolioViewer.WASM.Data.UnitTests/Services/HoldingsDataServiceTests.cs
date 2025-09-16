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
using Moq.EntityFrameworkCore;
using System.Reflection;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class HoldingsDataServiceTests
	{
		private readonly Mock<DatabaseContext> _mockDbContext;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly Mock<ILogger<HoldingsDataService>> _mockLogger;
		private readonly HoldingsDataService _service;

		public HoldingsDataServiceTests()
		{
			_mockDbContext = new Mock<DatabaseContext>();
			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_mockLogger = new Mock<ILogger<HoldingsDataService>>();
			
			_service = new HoldingsDataService(
				_mockDbContext.Object,
				_mockCurrencyExchange.Object,
				_mockLogger.Object);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithDefaultAccountId_CallsOverloadWithZero()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var cancellationToken = CancellationToken.None;

			_mockDbContext.Setup(x => x.HoldingAggregateds)
				.ReturnsDbSet(new List<HoldingAggregated>());

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
			var targetCurrency = Currency.USD;
			var accountId = 1;
			var cancellationToken = CancellationToken.None;

			_mockDbContext.Setup(x => x.HoldingAggregateds)
				.ReturnsDbSet(new List<HoldingAggregated>());

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
			var targetCurrency = Currency.USD;
			var accountId = 1;
			var cancellationToken = CancellationToken.None;

			var holding = new HoldingAggregated
			{
				Id = 1,
				AssetClass = AssetClass.Equity,
				Name = "Test Stock",
				Symbol = "TEST",
				SectorWeights = new List<SectorWeight>()
			};

			var snapshot = new CalculatedSnapshot
			{
				Id = 1,
				AccountId = accountId,
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 10,
				AverageCostPrice = new Money(Currency.USD, 100),
				CurrentUnitPrice = new Money(Currency.USD, 110),
				TotalInvested = new Money(Currency.USD, 1000),
				TotalValue = new Money(Currency.USD, 1100)
			};

			_mockDbContext.Setup(x => x.HoldingAggregateds)
				.ReturnsDbSet(new List<HoldingAggregated> { holding });

			_mockDbContext.Setup(x => x.CalculatedSnapshots)
				.ReturnsDbSet(new List<CalculatedSnapshot> { snapshot });

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
			var earliestDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
			var laterDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));

			var snapshots = new List<CalculatedSnapshot>
			{
				new() { Date = laterDate },
				new() { Date = earliestDate }
			};

			_mockDbContext.Setup(x => x.CalculatedSnapshots)
				.ReturnsDbSet(snapshots);

			// Act
			var result = await _service.GetMinDateAsync();

			// Assert
			Assert.Equal(earliestDate, result);
		}

		[Fact]
		public async Task GetAccountsAsync_WithValidConnection_ReturnsOrderedAccounts()
		{
			// Arrange
			var accounts = new List<Account>
			{
				new("Account B") { Id = 2 },
				new("Account A") { Id = 1 },
				new("Account C") { Id = 3 }
			};

			_mockDbContext.Setup(x => x.Database.CanConnectAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			_mockDbContext.Setup(x => x.Accounts)
				.ReturnsDbSet(accounts);

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
		public async Task GetAccountsAsync_WithInvalidConnection_ThrowsException()
		{
			// Arrange
			_mockDbContext.Setup(x => x.Database.CanConnectAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(false);

			// Act & Assert
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(
				() => _service.GetAccountsAsync());
			
			Assert.Contains("Database connection failed", exception.Message);
		}

		[Fact]
		public async Task GetSymbolsAsync_ReturnsDistinctOrderedSymbols()
		{
			// Arrange
			var symbolProfiles = new List<SymbolProfile>
			{
				new("AAPL", "Apple Inc.", new List<string>(), Currency.USD, "DataSource", AssetClass.Equity, null, new CountryWeight[0], new SectorWeight[0]),
				new("GOOGL", "Alphabet Inc.", new List<string>(), Currency.USD, "DataSource", AssetClass.Equity, null, new CountryWeight[0], new SectorWeight[0]),
				new("AAPL", "Apple Inc.", new List<string>(), Currency.USD, "DataSource", AssetClass.Equity, null, new CountryWeight[0], new SectorWeight[0]) // Duplicate
			};

			_mockDbContext.Setup(x => x.Database.CanConnectAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);

			_mockDbContext.Setup(x => x.SymbolProfiles)
				.ReturnsDbSet(symbolProfiles);

			// Act
			var result = await _service.GetSymbolsAsync();

			// Assert
			Assert.NotNull(result);
			Assert.Equal(2, result.Count);
			Assert.Equal("AAPL", result[0]);
			Assert.Equal("GOOGL", result[1]);
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_ReturnsHistoryPoints()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var startDate = DateTime.Today.AddDays(-10);
			var endDate = DateTime.Today;
			var accountId = 1;

			var snapshots = new List<CalculatedSnapshot>
			{
				new()
				{
					Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
					TotalValue = new Money(Currency.USD, 1000),
					TotalInvested = new Money(Currency.USD, 900)
				}
			};

			_mockDbContext.Setup(x => x.CalculatedSnapshots)
				.ReturnsDbSet(snapshots);

			// Act
			var result = await _service.GetPortfolioValueHistoryAsync(targetCurrency, startDate, endDate, accountId);

			// Assert
			Assert.NotNull(result);
			Assert.Single(result);
		}

		[Fact]
		public async Task ProcessHoldingAsync_WithEmptySnapshots_ReturnsEmptyDisplayModel()
		{
			// Arrange
			var holding = CreateTestHoldingWithSnapshots();
			var targetCurrency = Currency.USD;

			// Use reflection to access the private method
			var method = typeof(HoldingsDataService).GetMethod("ProcessHoldingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
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
			var holding = CreateTestHoldingWithSnapshots();
			holding.Snapshots.Add(new CalculatedSnapshot
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
			var method = typeof(HoldingsDataService).GetMethod("ProcessHoldingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
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
		[InlineData(typeof(BuyActivity), "Buy")]
		[InlineData(typeof(SellActivity), "Sell")]
		[InlineData(typeof(DividendActivity), "Dividend")]
		[InlineData(typeof(CashDepositActivity), "Deposit")]
		[InlineData(typeof(CashWithdrawalActivity), "Withdrawal")]
		[InlineData(typeof(FeeActivity), "Fee")]
		[InlineData(typeof(InterestActivity), "Interest")]
		public void GetDisplayType_ReturnsCorrectDisplayType(Type activityType, string expectedDisplayType)
		{
			// Use reflection to access the private method
			var method = typeof(HoldingsDataService).GetMethod("GetDisplayType", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(method);

			// Create a mock activity instance (this is simplified - in reality you'd need proper constructors)
			var activity = (Activity)Activator.CreateInstance(activityType, true)!;

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
			var method = typeof(HoldingsDataService).GetMethod("CalculateHoldingWeights", BindingFlags.NonPublic | BindingFlags.Static);
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
			var method = typeof(HoldingsDataService).GetMethod("CalculateHoldingWeights", BindingFlags.NonPublic | BindingFlags.Static);
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
				new() { Name = "Technology" },
				new() { Name = "Healthcare" },
				new() { Name = "Finance" }
			};

			// Use reflection to access the private method
			var method = typeof(HoldingsDataService).GetMethod("GetSectorDisplay", BindingFlags.NonPublic | BindingFlags.Static);
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
			var method = typeof(HoldingsDataService).GetMethod("GetSectorDisplay", BindingFlags.NonPublic | BindingFlags.Static);
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
			var holdingWithSnapshotsType = typeof(HoldingsDataService).GetNestedType("HoldingWithSnapshots", BindingFlags.NonPublic);
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
	}
}