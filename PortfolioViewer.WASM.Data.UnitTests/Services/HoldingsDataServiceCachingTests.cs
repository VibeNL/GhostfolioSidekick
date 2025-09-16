using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using System.Reflection;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.UnitTests.Services
{
	/// <summary>
	/// Unit tests specifically focused on currency conversion caching functionality
	/// </summary>
	public class HoldingsDataServiceCachingTests
	{
		private readonly Mock<DatabaseContext> _mockDbContext;
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly Mock<ILogger<HoldingsDataService>> _mockLogger;
		private readonly HoldingsDataService _service;

		public HoldingsDataServiceCachingTests()
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
		public async Task ConvertMoneyWithCache_FirstCall_CallsCurrencyExchangeAndCaches()
		{
			// Arrange
			var money = new Money(Currency.EUR, 100);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);
			var expectedResult = new Money(Currency.USD, 110);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(money, targetCurrency, date))
				.ReturnsAsync(expectedResult);

			// Use reflection to access the private method
			var method = typeof(HoldingsDataService).GetMethod("ConvertMoneyWithCache", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<Money>)method.Invoke(_service, new object[] { money, targetCurrency, date })!;
			var result = await task;

			// Assert
			Assert.Equal(expectedResult, result);
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(money, targetCurrency, date), Times.Once);
		}

		[Fact]
		public async Task ConvertMoneyWithCache_SecondCallWithinCacheTime_UsesCacheAndDoesNotCallExchange()
		{
			// Arrange
			var money1 = new Money(Currency.EUR, 100);
			var money2 = new Money(Currency.EUR, 200); // Different amount, same currency pair
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);
			var exchangeResult = new Money(Currency.USD, 110);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(money1, targetCurrency, date))
				.ReturnsAsync(exchangeResult);

			var method = typeof(HoldingsDataService).GetMethod("ConvertMoneyWithCache", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act - First call
			var task1 = (Task<Money>)method.Invoke(_service, new object[] { money1, targetCurrency, date })!;
			var result1 = await task1;

			// Act - Second call with different amount but same currency pair and date
			var task2 = (Task<Money>)method.Invoke(_service, new object[] { money2, targetCurrency, date })!;
			var result2 = await task2;

			// Assert
			Assert.Equal(new Money(Currency.USD, 110), result1);
			Assert.Equal(new Money(Currency.USD, 220), result2); // 200 * 1.1 rate = 220
			
			// Should only call currency exchange once (for the first call)
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Once);
		}

		[Fact]
		public async Task ConvertMoneyWithCache_WithSameCurrency_ReturnsOriginalWithoutCaching()
		{
			// Arrange
			var money = new Money(Currency.USD, 100);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);

			var method = typeof(HoldingsDataService).GetMethod("ConvertMoneyWithCache", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<Money>)method.Invoke(_service, new object[] { money, targetCurrency, date })!;
			var result = await task;

			// Assert
			Assert.Equal(money, result);
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>()), Times.Never);
		}

		[Fact]
		public void IsCacheValid_WithRecentTimestamp_ReturnsTrue()
		{
			// Arrange
			var cacheKey = "EUR_USD_2024-01-01";
			
			// Use reflection to access private fields and methods
			var cacheTimestampsField = typeof(HoldingsDataService).GetField("_cacheTimestamps", BindingFlags.NonPublic | BindingFlags.Instance);
			var isCacheValidMethod = typeof(HoldingsDataService).GetMethod("IsCacheValid", BindingFlags.NonPublic | BindingFlags.Instance);
			
			Assert.NotNull(cacheTimestampsField);
			Assert.NotNull(isCacheValidMethod);

			var cacheTimestamps = (ConcurrentDictionary<string, DateTime>)cacheTimestampsField.GetValue(_service)!;
			cacheTimestamps[cacheKey] = DateTime.UtcNow.AddMinutes(-1); // 1 minute ago

			// Act
			var result = (bool)isCacheValidMethod.Invoke(_service, new object[] { cacheKey })!;

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void IsCacheValid_WithExpiredTimestamp_ReturnsFalse()
		{
			// Arrange
			var cacheKey = "EUR_USD_2024-01-01";
			
			// Use reflection to access private fields and methods
			var cacheTimestampsField = typeof(HoldingsDataService).GetField("_cacheTimestamps", BindingFlags.NonPublic | BindingFlags.Instance);
			var isCacheValidMethod = typeof(HoldingsDataService).GetMethod("IsCacheValid", BindingFlags.NonPublic | BindingFlags.Instance);
			
			Assert.NotNull(cacheTimestampsField);
			Assert.NotNull(isCacheValidMethod);

			var cacheTimestamps = (ConcurrentDictionary<string, DateTime>)cacheTimestampsField.GetValue(_service)!;
			cacheTimestamps[cacheKey] = DateTime.UtcNow.AddMinutes(-10); // 10 minutes ago (expired)

			// Act
			var result = (bool)isCacheValidMethod.Invoke(_service, new object[] { cacheKey })!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void IsCacheValid_WithNonExistentKey_ReturnsFalse()
		{
			// Arrange
			var cacheKey = "NON_EXISTENT_KEY";
			
			var isCacheValidMethod = typeof(HoldingsDataService).GetMethod("IsCacheValid", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(isCacheValidMethod);

			// Act
			var result = (bool)isCacheValidMethod.Invoke(_service, new object[] { cacheKey })!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public async Task ConvertMoneyWithCache_WithZeroAmount_HandlesCorrectly()
		{
			// Arrange
			var money = new Money(Currency.EUR, 0);
			var targetCurrency = Currency.USD;
			var date = DateOnly.FromDateTime(DateTime.Today);
			var exchangeResult = new Money(Currency.USD, 0);

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(money, targetCurrency, date))
				.ReturnsAsync(exchangeResult);

			var method = typeof(HoldingsDataService).GetMethod("ConvertMoneyWithCache", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<Money>)method.Invoke(_service, new object[] { money, targetCurrency, date })!;
			var result = await task;

			// Assert
			Assert.Equal(exchangeResult, result);
		}

		[Fact]
		public async Task PreCacheCurrencyConversionsAsync_WithMultipleCurrencies_CachesAllRates()
		{
			// Arrange
			var holdings = new List<object>
			{
				CreateHoldingWithSnapshots("AAPL", Currency.USD, 100m),
				CreateHoldingWithSnapshots("ASML", Currency.EUR, 200m),
				CreateHoldingWithSnapshots("NESN", Currency.EUR, 300m) // Use EUR instead of CHF since CHF doesn't exist in Currency
			};
			
			var targetCurrency = Currency.USD;

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount * 1.1m));

			var method = typeof(HoldingsDataService).GetMethod("PreCacheCurrencyConversionsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task)method.Invoke(_service, new object[] { holdings, targetCurrency })!;
			await task;

			// Assert
			// Verify that currency exchange was called for EUR (but not USD since it's the target)
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(
				It.Is<Money>(m => m.Currency == Currency.EUR), targetCurrency, It.IsAny<DateOnly>()), Times.Once);
		}

		[Fact]
		public async Task PreCacheCurrencyConversionsAsync_WithCurrencyExchangeException_ContinuesWithOtherCurrencies()
		{
			// Arrange
			var holdings = new List<object>
			{
				CreateHoldingWithSnapshots("AAPL", Currency.USD, 100m),
				CreateHoldingWithSnapshots("ASML", Currency.EUR, 200m)
			};
			
			var targetCurrency = Currency.USD;

			_mockCurrencyExchange.Setup(x => x.ConvertMoney(
				It.Is<Money>(m => m.Currency == Currency.EUR), targetCurrency, It.IsAny<DateOnly>()))
				.ThrowsAsync(new Exception("Exchange rate not available"));

			var method = typeof(HoldingsDataService).GetMethod("PreCacheCurrencyConversionsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act & Assert - Should not throw
			var task = (Task)method.Invoke(_service, new object[] { holdings, targetCurrency })!;
			await task;

			// Verify that the warning was logged
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to cache currency conversion")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		/// <summary>
		/// Helper method to create a mock HoldingWithSnapshots object
		/// </summary>
		private static object CreateHoldingWithSnapshots(string symbol, Currency currency, decimal amount)
		{
			var holdingType = typeof(HoldingsDataService).GetNestedType("HoldingWithSnapshots", BindingFlags.NonPublic);
			Assert.NotNull(holdingType);

			var holding = Activator.CreateInstance(holdingType);
			Assert.NotNull(holding);

			// Create a snapshot with the specified currency
			var snapshot = new CalculatedSnapshot
			{
				Date = DateOnly.FromDateTime(DateTime.Today),
				TotalValue = new Money(currency, amount),
				CurrentUnitPrice = new Money(currency, amount),
				AverageCostPrice = new Money(currency, amount),
				TotalInvested = new Money(currency, amount),
				Quantity = 1
			};

			var snapshots = new List<CalculatedSnapshot> { snapshot };

			// Set properties using reflection
			holdingType.GetProperty("Symbol")!.SetValue(holding, symbol);
			holdingType.GetProperty("Snapshots")!.SetValue(holding, snapshots);

			return holding;
		}
	}
}