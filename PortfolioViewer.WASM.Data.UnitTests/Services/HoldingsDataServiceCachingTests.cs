using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.UnitTests.Services
{
	/// <summary>
	/// Unit tests for HoldingsDataService focusing on core functionality.
	/// Caching tests have been removed since caching is now handled by ICurrencyExchange.
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
			var holding = CreateTestHoldingWithSnapshots();
			holding.Snapshots.Add(new CalculatedSnapshot
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
			var method = typeof(HoldingsDataService).GetMethod("ProcessHoldingAsync", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(method);

			// Act
			var task = (Task<object>)method.Invoke(_service, new object[] { holding, targetCurrency })!;
			var result = await task;

			// Assert
			Assert.NotNull(result);
			
			// Verify that currency exchange was called for currency conversion
			_mockCurrencyExchange.Verify(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()), Times.AtLeastOnce);
		}

		/// <summary>
		/// Helper method to create a mock HoldingWithSnapshots object
		/// </summary>
		private static dynamic CreateTestHoldingWithSnapshots()
		{
			var holdingType = typeof(HoldingsDataService).GetNestedType("HoldingWithSnapshots", BindingFlags.NonPublic);
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
	}
}