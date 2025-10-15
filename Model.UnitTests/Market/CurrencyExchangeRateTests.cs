using AwesomeAssertions;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.Model.UnitTests.Market
{
	public class CurrencyExchangeRateTests
	{
		[Fact]
		public void Constructor_WithValidParameters_ShouldSetAllProperties()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new CurrencyExchangeRate(close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Close.Should().Be(close);
			marketData.Open.Should().Be(open);
			marketData.High.Should().Be(high);
			marketData.Low.Should().Be(low);
			marketData.TradingVolume.Should().Be(tradingVolume);
			marketData.Date.Should().Be(date);
		}

		[Fact]
		public void Constructor_WithNullClose_ShouldThrowArgumentNullException()
		{
			// Arrange
			Money? close = null;
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() =>
				new CurrencyExchangeRate(close!, open, high, low, tradingVolume, date));
		}

		[Fact]
		public void Constructor_WithNullOpen_ShouldThrowArgumentNullException()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			Money? open = null;
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() =>
				new CurrencyExchangeRate(close, open!, high, low, tradingVolume, date));
		}

		[Fact]
		public void Constructor_WithNullHigh_ShouldThrowArgumentNullException()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			Money? high = null;
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() =>
				new CurrencyExchangeRate(close, open, high!, low, tradingVolume, date));
		}

		[Fact]
		public void Constructor_WithNullLow_ShouldThrowArgumentNullException()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			Money? low = null;
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act & Assert
			Assert.Throws<ArgumentNullException>(() =>
				new CurrencyExchangeRate(close, open, high, low!, tradingVolume, date));
		}

		[Fact]
		public void DefaultConstructor_ShouldInitializePropertiesWithDefaults()
		{
			// Act
			var marketData = new CurrencyExchangeRate();

			// Assert
			marketData.Close.Should().BeNull();
			marketData.Open.Should().BeNull();
			marketData.High.Should().BeNull();
			marketData.Low.Should().BeNull();
			marketData.TradingVolume.Should().Be(0m);
			marketData.Date.Should().Be(default);
		}

		[Fact]
		public void CopyFrom_WithValidMarketData_ShouldCopyAllProperties()
		{
			// Arrange
			var originalClose = new Money(Currency.USD, 100m);
			var originalOpen = new Money(Currency.USD, 95m);
			var originalHigh = new Money(Currency.USD, 105m);
			var originalLow = new Money(Currency.USD, 90m);
			var originalTradingVolume = 1000000m;
			var originalDate = DateOnly.FromDateTime(DateTime.Now);

			var sourceMarketData = new CurrencyExchangeRate(originalClose, originalOpen, originalHigh, originalLow, originalTradingVolume, originalDate);
			var targetMarketData = new CurrencyExchangeRate();

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
			var initialClose = new Money(Currency.EUR, 50m);
			var initialOpen = new Money(Currency.EUR, 45m);
			var initialHigh = new Money(Currency.EUR, 55m);
			var initialLow = new Money(Currency.EUR, 40m);
			var initialTradingVolume = 500000m;
			var initialDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

			var newClose = new Money(Currency.USD, 100m);
			var newOpen = new Money(Currency.USD, 95m);
			var newHigh = new Money(Currency.USD, 105m);
			var newLow = new Money(Currency.USD, 90m);
			var newTradingVolume = 1000000m;
			var newDate = DateOnly.FromDateTime(DateTime.Now);

			var targetMarketData = new CurrencyExchangeRate(initialClose, initialOpen, initialHigh, initialLow, initialTradingVolume, initialDate);
			var sourceMarketData = new CurrencyExchangeRate(newClose, newOpen, newHigh, newLow, newTradingVolume, newDate);

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
		public void Properties_ShouldBeSettable()
		{
			// Arrange
			var marketData = new CurrencyExchangeRate();
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			marketData.Close = close;
			marketData.Open = open;
			marketData.High = high;
			marketData.Low = low;
			marketData.TradingVolume = tradingVolume;
			marketData.Date = date;

			// Assert
			marketData.Close.Should().Be(close);
			marketData.Open.Should().Be(open);
			marketData.High.Should().Be(high);
			marketData.Low.Should().Be(low);
			marketData.TradingVolume.Should().Be(tradingVolume);
			marketData.Date.Should().Be(date);
		}

		[Fact]
		public void Constructor_WithZeroTradingVolume_ShouldAcceptValue()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 0m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new CurrencyExchangeRate(close, open, high, low, tradingVolume, date);

			// Assert
			marketData.TradingVolume.Should().Be(0m);
		}

		[Fact]
		public void Constructor_WithNegativeTradingVolume_ShouldAcceptValue()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = -100m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new CurrencyExchangeRate(close, open, high, low, tradingVolume, date);

			// Assert
			marketData.TradingVolume.Should().Be(-100m);
		}

		[Fact]
		public void Constructor_WithDifferentCurrencies_ShouldAcceptValues()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.EUR, 95m);
			var high = new Money(Currency.GBP, 105m);
			var low = new Money(Currency.GBp, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.FromDateTime(DateTime.Now);

			// Act
			var marketData = new CurrencyExchangeRate(close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Close.Currency.Should().Be(Currency.USD);
			marketData.Open.Currency.Should().Be(Currency.EUR);
			marketData.High.Currency.Should().Be(Currency.GBP);
			marketData.Low.Currency.Should().Be(Currency.GBp);
		}

		[Fact]
		public void Constructor_WithMinDate_ShouldAcceptValue()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.MinValue;

			// Act
			var marketData = new CurrencyExchangeRate(close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Date.Should().Be(DateOnly.MinValue);
		}

		[Fact]
		public void Constructor_WithMaxDate_ShouldAcceptValue()
		{
			// Arrange
			var close = new Money(Currency.USD, 100m);
			var open = new Money(Currency.USD, 95m);
			var high = new Money(Currency.USD, 105m);
			var low = new Money(Currency.USD, 90m);
			var tradingVolume = 1000000m;
			var date = DateOnly.MaxValue;

			// Act
			var marketData = new CurrencyExchangeRate(close, open, high, low, tradingVolume, date);

			// Assert
			marketData.Date.Should().Be(DateOnly.MaxValue);
		}
	}
}
