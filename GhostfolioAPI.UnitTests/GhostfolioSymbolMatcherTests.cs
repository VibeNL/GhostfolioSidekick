using AwesomeAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{
	public class GhostfolioSymbolMatcherTests : IDisposable
	{
		private readonly Mock<IApplicationSettings> _settingsMock;
		private readonly Mock<IApiWrapper> _apiWrapperMock;
		private readonly MemoryCache _memoryCache;
		private readonly GhostfolioSymbolMatcher _symbolMatcher;
		private readonly ConfigurationInstance _configInstance;
		private bool _disposed;

		public GhostfolioSymbolMatcherTests()
		{
			_settingsMock = new Mock<IApplicationSettings>();
			_apiWrapperMock = new Mock<IApiWrapper>();
			_memoryCache = new MemoryCache(new MemoryCacheOptions());

			// Create real configuration objects instead of mocking
			_configInstance = new ConfigurationInstance
			{
				Settings = new Settings { DataProviderPreference = "YAHOO,COINGECKO", PrimaryCurrency = "EUR" }
			};

			_settingsMock.Setup(x => x.ConfigurationInstance).Returns(_configInstance);

			_symbolMatcher = new GhostfolioSymbolMatcher(_settingsMock.Object, _apiWrapperMock.Object, _memoryCache);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				_memoryCache.Dispose();
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		[Fact]
		public void DataSource_ShouldReturnGhostfolio()
		{
			// Act
			var result = _symbolMatcher.DataSource;

			// Assert
			result.Should().Be(Datasource.GHOSTFOLIO);
		}

		[Fact]
		public void Constructor_ShouldThrowArgumentNullException_WhenSettingsIsNull()
		{
			// Act & Assert
			Assert.Throws<ArgumentNullException>(() => new GhostfolioSymbolMatcher(null!, _apiWrapperMock.Object, _memoryCache));
		}

		[Fact]
		public void Constructor_ShouldThrowArgumentNullException_WhenApiWrapperIsNull()
		{
			// Act & Assert
			Assert.Throws<ArgumentNullException>(() => new GhostfolioSymbolMatcher(_settingsMock.Object, null!, _memoryCache));
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturnNull_WhenSymbolIdentifiersIsNull()
		{
			// Act
			var result = await _symbolMatcher.MatchSymbol(null!);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturnNull_WhenSymbolIdentifiersIsEmpty()
		{
			// Act
			var result = await _symbolMatcher.MatchSymbol([]);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturnNull_WhenIdentifierIsEmpty()
		{
			// Arrange
			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = string.Empty } };

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().BeNull();
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturnCachedResult_WhenSymbolIsCached()
		{
			// Arrange
			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = "AAPL" } };
			var expectedSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			// Cache the symbol first
			_memoryCache.Set("AAPL", expectedSymbol);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().Be(expectedSymbol);
			_apiWrapperMock.Verify(x => x.GetSymbolProfile(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturnSymbol_WhenFoundByApiWrapper()
		{
			// Arrange
			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = "AAPL" } };
			var expectedSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([expectedSymbol]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.Symbol.Should().Be("AAPL");
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("AAPL", false), Times.Once);
		}

		[Fact]
		public async Task MatchSymbol_ShouldHandleCryptocurrency_AddingUSDSuffix()
		{
			// Arrange
			var symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier
				{
					Identifier = "BTC",
					AllowedAssetSubClasses = [AssetSubClass.CryptoCurrency]
				}
			};

			var cryptoSymbol = new SymbolProfile
			{
				Symbol = "BTCUSD",
				Name = "Bitcoin",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.CryptoCurrency
			};

			// Setup mocks for all possible calls the crypto logic makes
			_apiWrapperMock.Setup(x => x.GetSymbolProfile("BTC", false))
				.ReturnsAsync([]);
			_apiWrapperMock.Setup(x => x.GetSymbolProfile("BTCUSD", false))
				.ReturnsAsync([cryptoSymbol]);
			_apiWrapperMock.Setup(x => x.GetSymbolProfile("bitcoin", false))
				.ReturnsAsync([]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.Symbol.Should().Be("BTC-USD"); // Should be fixed by FixYahooCrypto method
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("BTC", false), Times.AtLeastOnce);
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("BTCUSD", false), Times.AtLeastOnce);
		}

		[Fact]
		public async Task MatchSymbol_ShouldCacheNullResult_WhenNoSymbolFound()
		{
			// Arrange
			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = "UNKNOWN" } };

			_apiWrapperMock.Setup(x => x.GetSymbolProfile(It.IsAny<string>(), false))
				.ReturnsAsync([]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().BeNull();

			// Verify cached
			var cachedResult = _memoryCache.TryGetValue("UNKNOWN", out var cachedValue);
			cachedResult.Should().BeTrue();
			cachedValue.Should().BeNull();
		}

		[Fact]
		public async Task MatchSymbol_ShouldFilterByAllowedAssetClasses()
		{
			// Arrange
			var symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier
				{
					Identifier = "AAPL",
					AllowedAssetClasses = [AssetClass.FixedIncome] // Only bonds allowed
				}
			};

			var stockSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity, // This is a stock, not a bond
				AssetSubClass = AssetSubClass.Stock
			};

			_apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([stockSymbol]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().BeNull(); // Should be filtered out
		}

		[Fact]
		public async Task MatchSymbol_ShouldFilterByAllowedAssetSubClasses()
		{
			// Arrange
			var symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier
				{
					Identifier = "AAPL",
					AllowedAssetSubClasses = [AssetSubClass.Bond] // Only bonds allowed
				}
			};

			var stockSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock // This is a stock, not a bond
			};

			_apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([stockSymbol]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().BeNull(); // Should be filtered out
		}

		[Fact]
		public async Task MatchSymbol_ShouldRetryApiCall_WhenInitialCallReturnsEmpty()
		{
			// Arrange
			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = "AAPL" } };
			var expectedSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			// Setup to return empty first 4 times, then return symbol on 5th call
			_apiWrapperMock.SetupSequence(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([])
				.ReturnsAsync([])
				.ReturnsAsync([])
				.ReturnsAsync([])
				.ReturnsAsync([expectedSymbol]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.Symbol.Should().Be("AAPL");
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("AAPL", false), Times.Exactly(5));
		}

		[Fact]
		public async Task MatchSymbol_ShouldPreferExactMatch_OverFuzzyMatch()
		{
			// Arrange
			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = "AAPL" } };

			var exactMatch = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			var fuzzyMatch = new SymbolProfile
			{
				Symbol = "APPL",
				Name = "Apple Computer",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([fuzzyMatch, exactMatch]); // Return fuzzy match first

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.Symbol.Should().Be("AAPL"); // Should prefer exact match
		}

		[Fact]
		public async Task MatchSymbol_ShouldPreferExpectedCurrency()
		{
			// Arrange - This test verifies that well-known currencies are preferred
			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = "AAPL" } };

			var usdSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			var nonWellKnownSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.GetCurrency("JPY"), // Non-well-known currency comes later in preference
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([nonWellKnownSymbol, usdSymbol]); // Return non-well-known first

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.Currency.Symbol.Should().Be("USD"); // Should prefer well-known currency
		}

		[Fact]
		public async Task MatchSymbol_ShouldPreferDataSourceByConfiguration()
		{
			// Arrange - Create a new symbol matcher with different preferences
			var configInstance = new ConfigurationInstance
			{
				Settings = new Settings { DataProviderPreference = "COINGECKO,YAHOO", PrimaryCurrency = "EUR" }
			};
			var settingsMock = new Mock<IApplicationSettings>();
			settingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var symbolMatcher = new GhostfolioSymbolMatcher(settingsMock.Object, _apiWrapperMock.Object, _memoryCache);

			var symbolIdentifiers = new[] { new PartialSymbolIdentifier { Identifier = "AAPL" } };

			var yahooSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.GHOSTFOLIO + "_" + Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			var coingeckoSymbol = new SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.GHOSTFOLIO + "_" + Datasource.COINGECKO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([yahooSymbol, coingeckoSymbol]);

			// Act
			var result = await symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.DataSource.Should().Be(Datasource.GHOSTFOLIO + "_" + Datasource.COINGECKO); // Should prefer COINGECKO based on config
		}

		[Fact]
		public async Task MatchSymbol_ShouldFixYahooCryptoSymbol()
		{
			// Arrange
			var symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier
				{
					Identifier = "BTC",
					AllowedAssetSubClasses = [AssetSubClass.CryptoCurrency]
				}
			};

			var yahooCryptoSymbol = new SymbolProfile
			{
				Symbol = "BTCUSD", // 6 characters, should be fixed to BTC-USD
				Name = "Bitcoin",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.CryptoCurrency
			};

			// Setup mocks for all possible calls
			_apiWrapperMock.Setup(x => x.GetSymbolProfile("BTC", false))
				.ReturnsAsync([]);
			_apiWrapperMock.Setup(x => x.GetSymbolProfile("BTCUSD", false))
				.ReturnsAsync([yahooCryptoSymbol]);
			_apiWrapperMock.Setup(x => x.GetSymbolProfile("bitcoin", false))
				.ReturnsAsync([]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.Symbol.Should().Be("BTC-USD");
		}

		[Fact]
		public async Task MatchSymbol_ShouldHandleCryptocurrencyWithDashes()
		{
			// Arrange
			var symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier
				{
					Identifier = "WBTC",
					AllowedAssetSubClasses = [AssetSubClass.CryptoCurrency]
				}
			};

			var cryptoSymbol = new SymbolProfile
			{
				Symbol = "WBTC",
				Name = "Wrapped Bitcoin",
				Currency = Currency.USD,
				DataSource = Datasource.COINGECKO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.CryptoCurrency
			};

			// Setup mocks for exact matches
			_apiWrapperMock.Setup(x => x.GetSymbolProfile("WBTC", false))
				.ReturnsAsync([cryptoSymbol]);

			// Act
			var result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			result.Should().NotBeNull();
			result!.Symbol.Should().Be("WBTC");
		}
	}
}