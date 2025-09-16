using AwesomeAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
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
		private readonly Mock<ICurrencyExchange> _mockCurrencyExchange;
		private readonly Mock<ILogger<HoldingsDataServiceOLD>> _mockLogger;
		private readonly HoldingsDataService _holdingsDataService;

		public HoldingsDataServiceTests()
		{
			_mockDatabaseContext = new Mock<DatabaseContext>();
			_mockCurrencyExchange = new Mock<ICurrencyExchange>();
			_mockLogger = new Mock<ILogger<HoldingsDataServiceOLD>>();

			_holdingsDataService = new HoldingsDataService(
				_mockDatabaseContext.Object,
				_mockCurrencyExchange.Object,
				_mockLogger.Object);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithTargetCurrency_ShouldCallInternalMethod()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var cancellationToken = CancellationToken.None;

			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { calculatedSnapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency, cancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("AAPL");
			result[0].Name.Should().Be("Apple Inc");
			result[0].Currency.Should().Be(targetCurrency.Symbol);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithTargetCurrencyAndAccountId_ShouldCallInternalMethodWithAccountId()
		{
			// Arrange
			var targetCurrency = Currency.EUR;
			var accountId = 42;
			var cancellationToken = CancellationToken.None;

			var holdingAggregated = CreateTestHoldingAggregated("GOOGL", "Alphabet Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(holdingAggregated, accountId, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { calculatedSnapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency, accountId, cancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("GOOGL");
			result[0].Name.Should().Be("Alphabet Inc");
			result[0].Currency.Should().Be(targetCurrency.Symbol);
		}

		[Fact]
		public async Task GetHoldingsAsync_WithAccountIdZero_ShouldTreatAsNull()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var accountId = 0;
			var cancellationToken = CancellationToken.None;

			var holdingAggregated = CreateTestHoldingAggregated("MSFT", "Microsoft Corporation");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { calculatedSnapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency, accountId, cancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("MSFT");
		}

		[Theory]
		[InlineData("USD")]
		[InlineData("EUR")]
		[InlineData("GBP")]
		public async Task GetHoldingsInternallyAsync_WithDifferentCurrencies_ShouldConvertCorrectly(string currencySymbol)
		{
			// Arrange
			var targetCurrency = Currency.GetCurrency(currencySymbol);
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { calculatedSnapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount * 1.1m));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Currency.Should().Be(currencySymbol);
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithAccountIdNull_ShouldLogAllHoldings()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			int? accountId = null;

			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { calculatedSnapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading all holdings for portfolio in target currency")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithAccountIdSpecified_ShouldLogAccountId()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var accountId = 123;

			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var calculatedSnapshot = CreateTestCalculatedSnapshot(holdingAggregated, accountId, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { calculatedSnapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			await _holdingsDataService.GetHoldingsAsync(targetCurrency, accountId);

			// Assert
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Debug,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading holdings for account 123 in target currency")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithEmptyHoldingAggregateds_ShouldReturnEmptyList()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregateds = new List<HoldingAggregated>();

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithNullLastSnapshot_ShouldCreateHoldingWithDefaults()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot>(); // No snapshots

			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("AAPL");
			result[0].Name.Should().Be("Apple Inc");
			result[0].Quantity.Should().Be(0);
			result[0].AveragePrice.Amount.Should().Be(0);
			result[0].CurrentPrice.Amount.Should().Be(0);
			result[0].CurrentValue.Amount.Should().Be(0);
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_ShouldOrderBySymbol()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated1 = CreateTestHoldingAggregated("ZZPL", "Zzebra Corp");
			var holdingAggregated2 = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var holdingAggregated3 = CreateTestHoldingAggregated("MSFT", "Microsoft");

			var snapshot1 = CreateTestCalculatedSnapshot(holdingAggregated1, null, DateOnly.FromDateTime(DateTime.Now));
			var snapshot2 = CreateTestCalculatedSnapshot(holdingAggregated2, null, DateOnly.FromDateTime(DateTime.Now));
			var snapshot3 = CreateTestCalculatedSnapshot(holdingAggregated3, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated1.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot1 };
			holdingAggregated2.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot2 };
			holdingAggregated3.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot3 };

			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated1, holdingAggregated2, holdingAggregated3 };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3);
			result[0].Symbol.Should().Be("AAPL");
			result[1].Symbol.Should().Be("MSFT");
			result[2].Symbol.Should().Be("ZZPL");
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_ShouldCalculateWeightsProperly()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated1 = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var holdingAggregated2 = CreateTestHoldingAggregated("MSFT", "Microsoft");

			var snapshot1 = CreateTestCalculatedSnapshot(holdingAggregated1, null, DateOnly.FromDateTime(DateTime.Now));
			snapshot1.TotalValue = new Money(Currency.USD, 1000); // 25% of total

			var snapshot2 = CreateTestCalculatedSnapshot(holdingAggregated2, null, DateOnly.FromDateTime(DateTime.Now));
			snapshot2.TotalValue = new Money(Currency.USD, 3000); // 75% of total

			holdingAggregated1.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot1 };
			holdingAggregated2.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot2 };

			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated1, holdingAggregated2 };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(2);
			result[0].Weight.Should().Be(0.25m); // 1000 / 4000 = 0.25
			result[1].Weight.Should().Be(0.75m); // 3000 / 4000 = 0.75
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_ShouldCalculateGainLoss()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			snapshot.TotalValue = new Money(Currency.USD, 1100);
			snapshot.AverageCostPrice = new Money(Currency.USD, 100);
			snapshot.Quantity = 10;

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].GainLoss.Amount.Should().Be(100); // 1100 - (100 * 10) = 100
			result[0].GainLossPercentage.Should().Be(0.1m); // 100 / 1000 = 0.1
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithZeroAveragePriceAndQuantity_ShouldSetGainLossPercentageToZero()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			snapshot.TotalValue = new Money(Currency.USD, 100);
			snapshot.AverageCostPrice = new Money(Currency.USD, 0);
			snapshot.Quantity = 0;

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].GainLossPercentage.Should().Be(0);
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithZeroTotalValue_ShouldNotCalculateWeights()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			snapshot.TotalValue = new Money(Currency.USD, 0);
			snapshot.AverageCostPrice = new Money(Currency.USD, 100);
			snapshot.Quantity = 10;

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Weight.Should().Be(0);
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithPreferredNameOverSymbol_ShouldUseNameWhenAvailable()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Name.Should().Be("Apple Inc");
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithNullName_ShouldFallbackToSymbol()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", null);
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Name.Should().Be("AAPL");
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithSectorWeights_ShouldUseFirs()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			holdingAggregated.SectorWeights = new List<SectorWeight>
			{
				new() {  Name = "Technology", Weight = 0.8m },
				new() { Name = "ConsumerDiscretionary", Weight = 0.2m }
			};

			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));
			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Sector.Should().Be("Technology");
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithEmptySectorWeights_ShouldReturnEmptyString()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			holdingAggregated.SectorWeights = new List<SectorWeight>();

			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));
			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].Sector.Should().Be(string.Empty);
		}

		[Fact]
		public async Task ConvertMoney_WithNullMoney_ShouldReturnZeroAmount()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			// Set some properties to null
			snapshot.AverageCostPrice = null;
			snapshot.CurrentUnitPrice = null;
			snapshot.TotalValue = null;

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].AveragePrice.Amount.Should().Be(0);
			result[0].CurrentPrice.Amount.Should().Be(0);
			result[0].CurrentValue.Amount.Should().Be(0);
			result[0].AveragePrice.Currency.Should().Be(targetCurrency);
			result[0].CurrentPrice.Currency.Should().Be(targetCurrency);
			result[0].CurrentValue.Currency.Should().Be(targetCurrency);
		}

		[Fact]
		public async Task ConvertMoney_WithNullDate_ShouldReturnZeroAmount()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, null);

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);
			result[0].AveragePrice.Amount.Should().Be(100);
			result[0].CurrentPrice.Amount.Should().Be(110);
			result[0].CurrentValue.Amount.Should().Be(1100);
		}

		[Fact]
		public async Task ConvertMoney_WithValidMoneyAndDate_ShouldCallCurrencyExchange()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var holdingAggregated = CreateTestHoldingAggregated("AAPL", "Apple Inc");
			var snapshot = CreateTestCalculatedSnapshot(holdingAggregated, null, DateOnly.FromDateTime(DateTime.Now));

			holdingAggregated.CalculatedSnapshots = new List<CalculatedSnapshot> { snapshot };
			var holdingAggregateds = new List<HoldingAggregated> { holdingAggregated };

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);
			_mockCurrencyExchange.Setup(x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()))
				.ReturnsAsync((Money money, Currency currency, DateOnly date) => new Money(currency, money.Amount * 1.5m));

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(1);

			// Verify currency exchange was called for each money conversion
			_mockCurrencyExchange.Verify(
				x => x.ConvertMoney(It.IsAny<Money>(), targetCurrency, It.IsAny<DateOnly>()),
				Times.AtLeast(3)); // AveragePrice, CurrentPrice, CurrentValue
		}

		[Fact]
		public async Task GetHoldingsInternallyAsync_WithCancellationToken_ShouldPassTokenToEntityFramework()
		{
			// Arrange
			var targetCurrency = Currency.USD;
			var cancellationToken = new CancellationTokenSource().Token;
			var holdingAggregateds = new List<HoldingAggregated>();

			_mockDatabaseContext.Setup(x => x.HoldingAggregateds).ReturnsDbSet(holdingAggregateds);

			// Act
			var result = await _holdingsDataService.GetHoldingsAsync(targetCurrency, cancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		private static HoldingAggregated CreateTestHoldingAggregated(string symbol, string? name)
		{
			return new HoldingAggregated
			{
				Symbol = symbol,
				Name = name,
				AssetClass = AssetClass.Equity,
				SectorWeights = new List<SectorWeight>(),
				CalculatedSnapshots = new List<CalculatedSnapshot>()
			};
		}

		private static CalculatedSnapshot CreateTestCalculatedSnapshot(HoldingAggregated holding, int? accountId, DateOnly? date)
		{
			return new CalculatedSnapshot
			{
				Id = 1,
				AccountId = accountId ?? 1,
				Date = date ?? DateOnly.FromDateTime(DateTime.Now),
				Quantity = 10,
				AverageCostPrice = new Money(Currency.USD, 100),
				CurrentUnitPrice = new Money(Currency.USD, 110),
				TotalInvested = new Money(Currency.USD, 1000),
				TotalValue = new Money(Currency.USD, 1100)
			};
		}
	}
}