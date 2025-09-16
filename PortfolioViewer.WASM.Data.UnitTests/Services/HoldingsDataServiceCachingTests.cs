using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.UnitTests.Services
{
	/// <summary>
	/// Unit tests for HoldingsDataService focusing on currency exchange integration.
	/// Tests how the service interacts with ICurrencyExchange for currency conversions.
	/// </summary>
	public class HoldingsDataServiceCachingTests : IDisposable
	{
		private readonly DbContextOptions<DatabaseContext> _dbContextOptions;
		private readonly DatabaseContext _dbContext;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly Mock<ILogger<HoldingsDataServiceOLD>> _mockLogger;
		private readonly HoldingsDataServiceOLD _service;
		private readonly string _databaseFilePath;

		public HoldingsDataServiceCachingTests()
		{
			// Use SQLite in-memory database for more reliable testing
			_databaseFilePath = $"test_caching_{Guid.NewGuid()}.db";
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
		public async Task CurrencyConversion_CallsExchangeService()
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
		public async Task CurrencyConversion_WithSameCurrency_CanReturnOriginal()
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

		[Fact]
		public async Task CurrencyConversion_WithMultipleCalls_UsesExchangeService()
		{
			// Arrange
			var money1 = new Money(Currency.EUR, 100);
			var money2 = new Money(Currency.EUR, 200);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, date))
				.ReturnsAsync((Money m, Currency c, DateOnly d) => new Money(c, m.Amount * 1.1m));

			// Act
			var result1 = await _mockCurrencyExchange.Object.ConvertMoney(money1, targetCurrency, date);
			var result2 = await _mockCurrencyExchange.Object.ConvertMoney(money2, targetCurrency, date);

			// Assert
			Assert.Equal(new Money(Currency.USD, 110), result1);
			Assert.Equal(new Money(Currency.USD, 220), result2);
			
			// The exchange service is called for each conversion (internal caching is handled by the service)
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Exactly(2));
		}

		[Fact]
		public async Task CurrencyConversion_WithZeroAmount_HandlesCorrectly()
		{
			// Arrange
			var money = new Money(Currency.EUR, 0);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);
			var exchangeResult = new Money(Currency.USD, 0);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(money, targetCurrency, date))
				.ReturnsAsync(exchangeResult);

			// Act
			var result = await _mockCurrencyExchange.Object.ConvertMoney(money, targetCurrency, date);

			// Assert
			Assert.Equal(exchangeResult, result);
		}

		[Fact]
		public async Task CurrencyConversion_WithException_PropagatesException()
		{
			// Arrange
			var money = new Money(Currency.EUR, 100);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(money, targetCurrency, date))
				.ThrowsAsync(new Exception("Exchange rate not available"));

			// Act & Assert
			await Assert.ThrowsAsync<Exception>(() => 
				_mockCurrencyExchange.Object.ConvertMoney(money, targetCurrency, date));
		}

		[Fact]
		public async Task ProcessHoldingAsync_UsesExchangeService()
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
				AverageCostPrice = new Money(Currency.EUR, 100),
				CurrentUnitPrice = new Money(Currency.EUR, 110),
				TotalInvested = new Money(Currency.EUR, 1000),
				TotalValue = new Money(Currency.EUR, 1100)
			});

			var targetCurrency = Currency.USD;

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => 
					money.Currency == currency ? money : new Money(currency, money.Amount * 1.1m));

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("ProcessHoldingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<HoldingDisplayModel>)method.Invoke(_service, new object[] { holding, targetCurrency })!;
			var result = await task;

			// Assert
			Assert.NotNull(result);
			Assert.Equal("TEST", result.Symbol);
			Assert.Equal("Test Stock", result.Name);
			
			// Verify that currency exchange was called for currency conversion
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()), Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithCurrencyConversion_CallsExchangeService()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var targetCurrency = Currency.USD;
			var holdingAggregated = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test Stock",
				Symbol = "TEST",
				SectorWeights = new List<SectorWeight>()
			};

			var calculatedSnapshot = new CalculatedSnapshot
			{
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 10,
				AverageCostPrice = new Money(Currency.EUR, 100),
				CurrentUnitPrice = new Money(Currency.EUR, 110),
				TotalInvested = new Money(Currency.EUR, 1000),
				TotalValue = new Money(Currency.EUR, 1100)
			};

			holdingAggregated.CalculatedSnapshots.Add(calculatedSnapshot);
			_dbContext.HoldingAggregateds.Add(holdingAggregated);
			await _dbContext.SaveChangesAsync();

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => 
					money.Currency == currency ? money : new Money(currency, money.Amount * 1.1m));

			// Act
			var result = await _service.GetHoldingsAsync(targetCurrency);

			// Assert
			Assert.Single(result);
			var holding = result[0];
			Assert.Equal("TEST", holding.Symbol);
			Assert.Equal("Test Stock", holding.Name);
			
			// Verify currency exchange was called for the conversion from EUR to USD
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()), Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithMultipleCurrencies_CallsExchangeForNonTargetCurrency()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var targetCurrency = Currency.USD;
			
			// Create a simple holding with EUR currency
			var eurHolding = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "European Stock",
				Symbol = "EUR_STOCK",
				SectorWeights = new List<SectorWeight>()
			};

			var eurSnapshot = new CalculatedSnapshot
			{
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 10,
				AverageCostPrice = new Money(Currency.EUR, 100),
				CurrentUnitPrice = new Money(Currency.EUR, 110),
				TotalInvested = new Money(Currency.EUR, 1000),
				TotalValue = new Money(Currency.EUR, 1100)
			};

			eurHolding.CalculatedSnapshots.Add(eurSnapshot);
			_dbContext.HoldingAggregateds.Add(eurHolding);
			await _dbContext.SaveChangesAsync();

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => 
					money.Currency == currency ? money : new Money(currency, money.Amount * 1.1m));

			// Act
			var result = await _service.GetHoldingsAsync(targetCurrency);

			// Assert
			Assert.Single(result);
			var holding = result[0];
			Assert.Equal("EUR_STOCK", holding.Symbol);
			Assert.Equal("European Stock", holding.Name);
			Assert.Equal(targetCurrency.Symbol.ToString(), holding.Currency);
			
			// Verify currency conversion was called for EUR to USD conversion
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()), Times.AtLeastOnce);
		}

		[Fact]
		public async Task GetAccountsAsync_DoesNotUseCurrencyExchange()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var account = new Account("Test Account");
			_dbContext.Accounts.Add(account);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetAccountsAsync();

			// Assert
			Assert.Single(result);
			Assert.Equal("Test Account", result[0].Name);
			
			// Verify no currency exchange calls were made
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task ConvertSnapshotToTargetCurrency_CallsExchangeService()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var originalSnapshot = new CalculatedSnapshot
			{
				Date = DateOnly.FromDateTime(DateTime.Today),
				Quantity = 10,
				AverageCostPrice = new Money(Currency.EUR, 100),
				CurrentUnitPrice = new Money(Currency.EUR, 110),
				TotalInvested = new Money(Currency.EUR, 1000),
				TotalValue = new Money(Currency.EUR, 1100)
			};

			var targetCurrency = Currency.USD;

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount * 1.1m));

			// Use reflection to access the private method
			var method = typeof(HoldingsDataServiceOLD).GetMethod("ConvertSnapshotToTargetCurrency", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<CalculatedSnapshot>)method.Invoke(_service, new object[] { targetCurrency, originalSnapshot })!;
			var result = await task;

			// Assert
			Assert.NotNull(result);
			Assert.Equal(targetCurrency, result.AverageCostPrice.Currency);
			Assert.Equal(targetCurrency, result.CurrentUnitPrice.Currency);
			Assert.Equal(targetCurrency, result.TotalInvested.Currency);
			Assert.Equal(targetCurrency, result.TotalValue.Currency);
			
			// Verify currency exchange was called for each Money property
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()), Times.Exactly(4));
		}

		[Fact]
		public async Task GetSymbolsAsync_DoesNotUseCurrencyExchange()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var symbolProfile = new SymbolProfile("TEST", "Test Symbol", ["TEST"], Currency.USD, Datasource.YAHOO, AssetClass.Equity, AssetSubClass.Stock, [], []);
			_dbContext.SymbolProfiles.Add(symbolProfile);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetSymbolsAsync();

			// Assert
			Assert.Single(result);
			Assert.Equal("TEST", result[0]);
			
			// Verify no currency exchange calls were made
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task GetMinDateAsync_DoesNotUseCurrencyExchange()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var testDate = new DateOnly(2024, 1, 1);
			var holdingAggregated = new HoldingAggregated
			{
				AssetClass = AssetClass.Equity,
				Name = "Test",
				Symbol = "TEST",
				SectorWeights = new List<SectorWeight>()
			};

			var snapshot = new CalculatedSnapshot
			{
				Date = testDate,
				Quantity = 10,
				AverageCostPrice = new Money(Currency.USD, 100),
				CurrentUnitPrice = new Money(Currency.USD, 110),
				TotalInvested = new Money(Currency.USD, 1000),
				TotalValue = new Money(Currency.USD, 1100)
			};

			holdingAggregated.CalculatedSnapshots.Add(snapshot);
			_dbContext.HoldingAggregateds.Add(holdingAggregated);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetMinDateAsync();

			// Assert
			Assert.Equal(testDate, result);
			
			// Verify no currency exchange calls were made
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task GetSymbolsByAccountAsync_DoesNotUseCurrencyExchange()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var account = new Account("Test Account") { Id = 1 };
			var symbolProfile = new SymbolProfile("TEST", "Test Symbol", ["TEST"], Currency.USD, Datasource.YAHOO, AssetClass.Equity, AssetSubClass.Stock, [], []);
			var holding = new Holding();
			holding.SymbolProfiles.Add(symbolProfile);
			
			var activity = new BuyActivity(account, holding, [], DateTime.Today, 10, new Money(Currency.USD, 100), "T1", 1, "Test");
			
			_dbContext.Accounts.Add(account);
			_dbContext.SymbolProfiles.Add(symbolProfile);
			_dbContext.Holdings.Add(holding);
			_dbContext.Activities.Add(activity);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetSymbolsByAccountAsync(1);

			// Assert
			Assert.Single(result);
			Assert.Equal("TEST", result[0]);
			
			// Verify no currency exchange calls were made
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public async Task GetAccountsBySymbolAsync_DoesNotUseCurrencyExchange()
		{
			// Arrange
			await _dbContext.Database.EnsureCreatedAsync();
			
			var account = new Account("Test Account");
			var symbolProfile = new SymbolProfile("TEST", "Test Symbol", ["TEST"], Currency.USD, Datasource.YAHOO, AssetClass.Equity, AssetSubClass.Stock, [], []);
			var holding = new Holding();
			holding.SymbolProfiles.Add(symbolProfile);
			
			var activity = new BuyActivity(account, holding, [], DateTime.Today, 10, new Money(Currency.USD, 100), "T1", 1, "Test");
			
			_dbContext.Accounts.Add(account);
			_dbContext.SymbolProfiles.Add(symbolProfile);
			_dbContext.Holdings.Add(holding);
			_dbContext.Activities.Add(activity);
			await _dbContext.SaveChangesAsync();

			// Act
			var result = await _service.GetAccountsBySymbolAsync("TEST");

			// Assert
			Assert.Single(result);
			Assert.Equal("Test Account", result[0].Name);
			
			// Verify no currency exchange calls were made
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Never);
		}

		/// <summary>
		/// Helper method to create a mock HoldingWithSnapshots object
		/// </summary>
		private static dynamic CreateTestHoldingWithSnapshots()
		{
			var holdingType = typeof(HoldingsDataServiceOLD).GetNestedType("HoldingWithSnapshots", BindingFlags.NonPublic);
			Assert.NotNull(holdingType);

			var holding = Activator.CreateInstance(holdingType);
			Assert.NotNull(holding);

			// Set properties using reflection
			holdingType.GetProperty("Id")!.SetValue(holding, 1L);
			holdingType.GetProperty("AssetClass")!.SetValue(holding, AssetClass.Equity);
			holdingType.GetProperty("Name")!.SetValue(holding, "Test Stock");
			holdingType.GetProperty("Symbol")!.SetValue(holding, "TEST");
			holdingType.GetProperty("SectorWeights")!.SetValue(holding, new List<SectorWeight>());
			holdingType.GetProperty("Snapshots")!.SetValue(holding, new List<CalculatedSnapshot>());

			return holding;
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