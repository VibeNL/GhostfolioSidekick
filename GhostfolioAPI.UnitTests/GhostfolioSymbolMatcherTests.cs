using AwesomeAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{
	public class GhostfolioSymbolMatcherTests
	{
		private readonly Mock<IApplicationSettings> _settingsMock;
		private readonly Mock<IApiWrapper> _apiWrapperMock;
		private readonly Mock<ILogger<GhostfolioSymbolMatcher>> _loggerMock;
		private readonly GhostfolioSymbolMatcher _symbolMatcher;
		private readonly ConfigurationInstance _configInstance;

		public GhostfolioSymbolMatcherTests()
		{
			_settingsMock = new Mock<IApplicationSettings>();
			_apiWrapperMock = new Mock<IApiWrapper>();
			_loggerMock = new Mock<ILogger<GhostfolioSymbolMatcher>>();

			// Create real configuration objects instead of mocking
			_configInstance = new ConfigurationInstance
			{
				Settings = new Settings { DataProviderPreference = "YAHOO,COINGECKO", PrimaryCurrency = "EUR" }
			};

			_ = _settingsMock.Setup(x => x.ConfigurationInstance).Returns(_configInstance);

			_symbolMatcher = new GhostfolioSymbolMatcher(_settingsMock.Object, _apiWrapperMock.Object);
		}

		[Fact]
		public void DataSource_ShouldReturnGhostfolio()
		{
			// Act
			string result = _symbolMatcher.DataSource;

			// Assert
			_ = result.Should().Be(Datasource.GHOSTFOLIO);
		}

		[Fact]
		public async Task Constructor_ShouldThrowArgumentNullException_WhenSettingsIsNull()
		{
			// Act & Assert
			_ = await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(new GhostfolioSymbolMatcher(null!, _apiWrapperMock.Object)));
		}

		[Fact]
		public async Task Constructor_ShouldThrowArgumentNullException_WhenApiWrapperIsNull()
		{
			// Act & Assert
			_ = await Assert.ThrowsAsync<ArgumentNullException>(() => Task.FromResult(new GhostfolioSymbolMatcher(_settingsMock.Object, null!)));
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturnNull_WhenSymbolIdentifiersIsNull()
		{
			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(null!);

			// Assert
			_ = result.Should().BeNull();
		}

		[Fact]
		public async Task MatchSymbol_ShouldReturnNull_WhenSymbolIdentifiersIsEmpty()
		{
			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol([]);

			// Assert
			_ = result.Should().BeNull();
		}


		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void CreateGeneric_ShouldReturnNull_WhenIdentifierIsNullOrWhitespace(string? identifier)
		{
			// CreateGeneric is the guard that prevents invalid identifiers from ever reaching
			// MatchSymbol. Callers that pass null/empty/whitespace receive null back, which
			// is filtered out before the matcher is invoked.
			PartialSymbolIdentifier? result = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, identifier, null);

			_ = result.Should().BeNull();
		}


		[Fact]
		public async Task MatchSymbol_ShouldReturnSymbol_WhenFoundByApiWrapper()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[] { PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "AAPL", null)! };
			SymbolProfile expectedSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([expectedSymbol]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Symbol.Should().Be("AAPL");
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("AAPL", false), Times.Once);
		}

		[Fact]
		public async Task MatchSymbol_ShouldHandleCryptocurrency_AddingUSDSuffix()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier(IdentifierType.Ticker, "BTC", null, [], [AssetSubClass.CryptoCurrency])
			};

			SymbolProfile cryptoSymbol = new()
			{
				Symbol = "BTCUSD",
				Name = "Bitcoin",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.CryptoCurrency
			};

			// Setup mocks for all possible calls the crypto logic makes
			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("BTC", false))
				.ReturnsAsync([]);
			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("BTCUSD", false))
				.ReturnsAsync([cryptoSymbol]);
			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("bitcoin", false))
				.ReturnsAsync([]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Symbol.Should().Be("BTC-USD"); // Should be fixed by FixYahooCrypto method
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("BTC", false), Times.AtLeastOnce);
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("BTCUSD", false), Times.AtLeastOnce);
		}

		[Fact]
		public async Task MatchSymbol_ShouldCacheNullResult_WhenNoSymbolFound()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[] { PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "UNKNOWN", null)! };

			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile(It.IsAny<string>(), false))
				.ReturnsAsync([]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().BeNull();
		}

		[Fact]
		public async Task MatchSymbol_ShouldFilterByAllowedAssetClasses()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier(IdentifierType.Ticker, "AAPL", null, [AssetClass.FixedIncome], []) // Only bonds allowed
			};

			SymbolProfile stockSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity, // This is a stock, not a bond
				AssetSubClass = AssetSubClass.Stock
			};

			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([stockSymbol]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().BeNull(); // Should be filtered out
		}

		[Fact]
		public async Task MatchSymbol_ShouldFilterByAllowedAssetSubClasses()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier(IdentifierType.Ticker, "AAPL", null, [], [AssetSubClass.Bond]) // Only bonds allowed
			};

			SymbolProfile stockSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock // This is a stock, not a bond
			};

			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([stockSymbol]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().BeNull(); // Should be filtered out
		}

		[Fact]
		public async Task MatchSymbol_ShouldRetryApiCall_WhenInitialCallReturnsEmpty()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[] { PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "AAPL", null)! };
			SymbolProfile expectedSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			// Setup to return empty first 4 times, then return symbol on 5th call
			_ = _apiWrapperMock.SetupSequence(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([])
				.ReturnsAsync([])
				.ReturnsAsync([])
				.ReturnsAsync([])
				.ReturnsAsync([expectedSymbol]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Symbol.Should().Be("AAPL");
			_apiWrapperMock.Verify(x => x.GetSymbolProfile("AAPL", false), Times.Exactly(5));
		}

		[Fact]
		public async Task MatchSymbol_ShouldPreferExactMatch_OverFuzzyMatch()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[] { PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "AAPL", null)! };

			SymbolProfile exactMatch = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			SymbolProfile fuzzyMatch = new()
			{
				Symbol = "APPL",
				Name = "Apple Computer",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([fuzzyMatch, exactMatch]); // Return fuzzy match first

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Symbol.Should().Be("AAPL"); // Should prefer exact match
		}

		[Fact]
		public async Task MatchSymbol_ShouldPreferExpectedCurrency()
		{
			// Arrange - This test verifies that well-known currencies are preferred
			PartialSymbolIdentifier[] symbolIdentifiers = new[] { PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "AAPL", null)! };

			SymbolProfile usdSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			SymbolProfile nonWellKnownSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.GetCurrency("JPY"), // Non-well-known currency comes later in preference
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([nonWellKnownSymbol, usdSymbol]); // Return non-well-known first

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Currency.Symbol.Should().Be("USD"); // Should prefer well-known currency
		}

		[Fact]
		public async Task MatchSymbol_ShouldPreferDataSourceByConfiguration()
		{
			// Arrange - Create a new symbol matcher with different preferences
			ConfigurationInstance configInstance = new()
			{
				Settings = new Settings { DataProviderPreference = "COINGECKO,YAHOO", PrimaryCurrency = "EUR" }
			};
			Mock<IApplicationSettings> settingsMock = new();
			_ = settingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			GhostfolioSymbolMatcher symbolMatcher = new(settingsMock.Object, _apiWrapperMock.Object);

			PartialSymbolIdentifier[] symbolIdentifiers = new[] { PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "AAPL", null)! };

			SymbolProfile yahooSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.GHOSTFOLIO + "_" + Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			SymbolProfile coingeckoSymbol = new()
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = Currency.USD,
				DataSource = Datasource.GHOSTFOLIO + "_" + Datasource.COINGECKO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.Stock
			};

			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("AAPL", false))
				.ReturnsAsync([yahooSymbol, coingeckoSymbol]);

			// Act
			SymbolProfile? result = await symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.DataSource.Should().Be(Datasource.GHOSTFOLIO + "_" + Datasource.COINGECKO); // Should prefer COINGECKO based on config
		}

		[Fact]
		public async Task MatchSymbol_ShouldFixYahooCryptoSymbol()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier(IdentifierType.Ticker, "BTC", null, [], [AssetSubClass.CryptoCurrency])
			};

			SymbolProfile yahooCryptoSymbol = new()
			{
				Symbol = "BTCUSD", // 6 characters, should be fixed to BTC-USD
				Name = "Bitcoin",
				Currency = Currency.USD,
				DataSource = Datasource.YAHOO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.CryptoCurrency
			};

			// Setup mocks for all possible calls
			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("BTC", false))
				.ReturnsAsync([]);
			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("BTCUSD", false))
				.ReturnsAsync([yahooCryptoSymbol]);
			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("bitcoin", false))
				.ReturnsAsync([]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Symbol.Should().Be("BTC-USD");
		}

		[Fact]
		public async Task MatchSymbol_ShouldHandleCryptocurrencyWithDashes()
		{
			// Arrange
			PartialSymbolIdentifier[] symbolIdentifiers = new[]
			{
				new PartialSymbolIdentifier(IdentifierType.Ticker, "WBTC", null, [], [AssetSubClass.CryptoCurrency])
			};

			SymbolProfile cryptoSymbol = new()
			{
				Symbol = "WBTC",
				Name = "Wrapped Bitcoin",
				Currency = Currency.USD,
				DataSource = Datasource.COINGECKO,
				AssetClass = AssetClass.Equity,
				AssetSubClass = AssetSubClass.CryptoCurrency
			};

			// Setup mocks for exact matches
			_ = _apiWrapperMock.Setup(x => x.GetSymbolProfile("WBTC", false))
				.ReturnsAsync([cryptoSymbol]);

			// Act
			SymbolProfile? result = await _symbolMatcher.MatchSymbol(symbolIdentifiers);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result!.Symbol.Should().Be("WBTC");
		}
	}

}
