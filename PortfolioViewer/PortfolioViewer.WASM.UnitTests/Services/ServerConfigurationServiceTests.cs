using AwesomeAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
	public class ServerConfigurationServiceTests
	{
		private readonly Mock<IApplicationSettings> _applicationSettingsMock;
		private readonly ServerConfigurationService _serverConfigurationService;

		public ServerConfigurationServiceTests()
		{
			_applicationSettingsMock = new Mock<IApplicationSettings>();
			_serverConfigurationService = new ServerConfigurationService(_applicationSettingsMock.Object);
		}

		[Fact]
		public void PrimaryCurrency_WhenConfigurationInstanceIsNull_ReturnsEUR()
		{
			// Arrange
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns((ConfigurationInstance)null!);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public void PrimaryCurrency_WhenSettingsIsNull_ReturnsEUR()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = null!
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public void PrimaryCurrency_WhenPrimaryCurrencyIsNull_ReturnsEUR()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = null!
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public void PrimaryCurrency_WhenPrimaryCurrencyIsEmpty_ReturnsEUR()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = string.Empty
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public void PrimaryCurrency_WhenPrimaryCurrencyIsWhitespace_ReturnsEUR()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "   "
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public void PrimaryCurrency_WhenValidUSDCurrency_ReturnsUSD()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "USD"
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.USD);
		}

		[Fact]
		public void PrimaryCurrency_WhenValidGBPCurrency_ReturnsGBP()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "GBP"
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.GBP);
		}

		[Fact]
		public void PrimaryCurrency_WhenValidEURCurrency_ReturnsEUR()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "EUR"
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public void PrimaryCurrency_WhenUnknownCurrency_CreatesNewCurrency()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "JPY"
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result.Symbol.Should().Be("JPY");
		}

		[Fact]
		public void PrimaryCurrency_WhenCalledMultipleTimes_ReturnsConsistentResults()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "USD"
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result1 = _serverConfigurationService.PrimaryCurrency;
			var result2 = _serverConfigurationService.PrimaryCurrency;
			var result3 = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result1.Should().Be(Currency.USD);
			result2.Should().Be(Currency.USD);
			result3.Should().Be(Currency.USD);
			result1.Should().Be(result2);
			result2.Should().Be(result3);
		}

		[Fact]
		public void PrimaryCurrency_WhenConfigurationChanges_ReflectsNewValue()
		{
			// Arrange
			var settings1 = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "USD"
			};
			var configurationInstance1 = new ConfigurationInstance
			{
				Settings = settings1
			};

			var settings2 = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "GBP"
			};
			var configurationInstance2 = new ConfigurationInstance
			{
				Settings = settings2
			};

			_applicationSettingsMock.SetupSequence(x => x.ConfigurationInstance)
				.Returns(configurationInstance1)
				.Returns(configurationInstance2);

			// Act
			var result1 = _serverConfigurationService.PrimaryCurrency;
			var result2 = _serverConfigurationService.PrimaryCurrency;

			// Assert
			result1.Should().Be(Currency.USD);
			result2.Should().Be(Currency.GBP);
		}

		[Fact]
		public void PrimaryCurrency_WhenCurrencyIsCaseInsensitive_ReturnsCorrectCurrency()
		{
			// Arrange
			var settings = new Settings
			{
				DataProviderPreference = "YAHOO",
				PrimaryCurrency = "usd"
			};
			var configurationInstance = new ConfigurationInstance
			{
				Settings = settings
			};
			_applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _serverConfigurationService.PrimaryCurrency;

			// Assert
			// Currency.GetCurrency is case sensitive, so "usd" would create a new currency
			result.Symbol.Should().Be("usd");
		}
	}
}