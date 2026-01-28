using AwesomeAssertions;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.Model.UnitTests.Market
{
	public class MarketDataTests
	{
		[Fact]
		public void Constructor_WithValidParameters_ShouldSetAllProperties()
		{
			// Arrange
			var currency = Currency.USD;
			var close = 100m;
			var open = 95m;
			var high = 105m;
			var low = 90m;
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new MarketData(currency, close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Currency.Should().Be(currency);
			marketData.Close.Should().Be(close);
			marketData.Open.Should().Be(open);
			marketData.High.Should().Be(high);
			marketData.Low.Should().Be(low);
			marketData.TradingVolume.Should().Be(tradingVolume);
			marketData.Date.Should().Be(date);
		}

		[Fact]
		public void DefaultConstructor_ShouldInitializePropertiesWithDefaults()
		{
			// Act
			var marketData = new MarketData();

			// Assert
			marketData.Currency.Should().Be(default(Currency));
			marketData.Close.Should().Be(0m);
			marketData.Open.Should().Be(0m);
			marketData.High.Should().Be(0m);
			marketData.Low.Should().Be(0m);
			marketData.TradingVolume.Should().Be(0m);
			marketData.Date.Should().Be(default);
			marketData.IsGenerated.Should().BeFalse();
		}

		[Fact]
		public void CopyFrom_WithValidMarketData_ShouldCopyAllProperties()
		{
			// Arrange
			var originalCurrency = Currency.USD;
			var originalClose = 100m;
			var originalOpen = 95m;
			var originalHigh = 105m;
			var originalLow = 90m;
			var originalTradingVolume = 1000000m;
			var originalDate = DateOnly.FromDateTime(DateTime.Now);

			var sourceMarketData = new MarketData(originalCurrency, originalClose, originalOpen, originalHigh, originalLow, originalTradingVolume, originalDate);
			var targetMarketData = new MarketData();

			// Act
			targetMarketData.CopyFrom(sourceMarketData);

			// Assert
			targetMarketData.Close.Should().Be(sourceMarketData.Close);
			targetMarketData.Open.Should().Be(sourceMarketData.Open);
			targetMarketData.High.Should().Be(sourceMarketData.High);
			targetMarketData.Low.Should().Be(sourceMarketData.Low);
			targetMarketData.TradingVolume.Should().Be(sourceMarketData.TradingVolume);
			targetMarketData.Date.Should().Be(sourceMarketData.Date);
		}

		[Fact]
		public void CopyFrom_ShouldOverwriteExistingValues()
		{
			// Arrange
			var initialCurrency = Currency.EUR;
			var initialClose = 50m;
			var initialOpen = 45m;
			var initialHigh = 55m;
			var initialLow = 40m;
			var initialTradingVolume = 500000m;
			var initialDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

			var newCurrency = Currency.USD;
			var newClose = 100m;
			var newOpen = 95m;
			var newHigh = 105m;
			var newLow = 90m;
			var newTradingVolume = 1000000m;
			var newDate = DateOnly.FromDateTime(DateTime.Now);

			var targetMarketData = new MarketData(initialCurrency, initialClose, initialOpen, initialHigh, initialLow, initialTradingVolume, initialDate);
			var sourceMarketData = new MarketData(newCurrency, newClose, newOpen, newHigh, newLow, newTradingVolume, newDate);

			// Act
			targetMarketData.CopyFrom(sourceMarketData);

			// Assert
			targetMarketData.Close.Should().Be(newClose);
			targetMarketData.Open.Should().Be(newOpen);
			targetMarketData.High.Should().Be(newHigh);
			targetMarketData.Low.Should().Be(newLow);
			targetMarketData.TradingVolume.Should().Be(newTradingVolume);
			targetMarketData.Date.Should().Be(newDate);
		}

		[Fact]
		public void CopyFrom_ShouldNotCopyCurrency()
		{
			// Arrange
			var initialCurrency = Currency.EUR;
			var initialClose = 50m;
			var initialOpen = 45m;
			var initialHigh = 55m;
			var initialLow = 40m;
			var initialTradingVolume = 500000m;
			var initialDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

			var newCurrency = Currency.USD;
			var newClose = 100m;
			var newOpen = 95m;
			var newHigh = 105m;
			var newLow = 90m;
			var newTradingVolume = 1000000m;
			var newDate = DateOnly.FromDateTime(DateTime.Now);

			var targetMarketData = new MarketData(initialCurrency, initialClose, initialOpen, initialHigh, initialLow, initialTradingVolume, initialDate);
			var sourceMarketData = new MarketData(newCurrency, newClose, newOpen, newHigh, newLow, newTradingVolume, newDate);

			// Act
			targetMarketData.CopyFrom(sourceMarketData);

			// Assert
			targetMarketData.Currency.Should().Be(initialCurrency); // Currency should NOT be copied
		}

		[Fact]
		public void Properties_ShouldBeSettable()
		{
			// Arrange
			var marketData = new MarketData();
			var currency = Currency.USD;
			var close = 100m;
			var open = 95m;
			var high = 105m;
			var low = 90m;
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			marketData.Currency = currency;
			marketData.Close = close;
			marketData.Open = open;
			marketData.High = high;
			marketData.Low = low;
			marketData.TradingVolume = tradingVolume;
			marketData.Date = date;
			marketData.IsGenerated = true;

			// Assert
			marketData.Currency.Should().Be(currency);
			marketData.Close.Should().Be(close);
			marketData.Open.Should().Be(open);
			marketData.High.Should().Be(high);
			marketData.Low.Should().Be(low);
			marketData.TradingVolume.Should().Be(tradingVolume);
			marketData.Date.Should().Be(date);
			marketData.IsGenerated.Should().BeTrue();
		}

		[Fact]
		public void Constructor_WithZeroTradingVolume_ShouldAcceptValue()
		{
			// Arrange
			var currency = Currency.USD;
			var close = 100m;
			var open = 95m;
			var high = 105m;
			var low = 90m;
			var tradingVolume = 0m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new MarketData(currency, close, open, high, low, tradingVolume, date);

			// Assert
			marketData.TradingVolume.Should().Be(0m);
		}

		[Fact]
		public void Constructor_WithNegativeTradingVolume_ShouldAcceptValue()
		{
			// Arrange
			var currency = Currency.USD;
			var close = 100m;
			var open = 95m;
			var high = 105m;
			var low = 90m;
			var tradingVolume = -100m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new MarketData(currency, close, open, high, low, tradingVolume, date);

			// Assert
			marketData.TradingVolume.Should().Be(-100m);
		}

		[Fact]
		public void Constructor_WithDifferentCurrencies_ShouldSetCurrency()
		{
			// Arrange
			var currency = Currency.EUR;
			var close = 100m;
			var open = 95m;
			var high = 105m;
			var low = 90m;
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new MarketData(currency, close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Currency.Should().Be(Currency.EUR);
		}

		[Fact]
		public void Constructor_WithMinDate_ShouldAcceptValue()
		{
			// Arrange
			var currency = Currency.USD;
			var close = 100m;
			var open = 95m;
			var high = 105m;
			var low = 90m;
			var tradingVolume = 1000000m;
			var date = DateOnly.MinValue;

			// Act
			var marketData = new MarketData(currency, close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Date.Should().Be(DateOnly.MinValue);
		}

		[Fact]
		public void Constructor_WithMaxDate_ShouldAcceptValue()
		{
			// Arrange
			var currency = Currency.USD;
			var close = 100m;
			var open = 95m;
			var high = 105m;
			var low = 90m;
			var tradingVolume = 1000000m;
			var date = DateOnly.MaxValue;

			// Act
			var marketData = new MarketData(currency, close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Date.Should().Be(DateOnly.MaxValue);
		}
	}
}
