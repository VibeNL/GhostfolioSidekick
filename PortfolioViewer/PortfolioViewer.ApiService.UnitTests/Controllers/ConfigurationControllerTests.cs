using AwesomeAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.PortfolioViewer.ApiService.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	public class ConfigurationControllerTests
	{
		private readonly Mock<IApplicationSettings> _mockApplicationSettings;
		private readonly ConfigurationController _controller;

		public ConfigurationControllerTests()
		{
			_mockApplicationSettings = new Mock<IApplicationSettings>();
			_controller = new ConfigurationController(_mockApplicationSettings.Object)
			{
				// Setup HttpContext to avoid null reference exceptions
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};
		}

		[Fact]
		public void GetPrimaryCurrency_WithValidConfiguration_ReturnsOkWithPrimaryCurrency()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = "USD"
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("USD");
		}

		[Fact]
		public void GetPrimaryCurrency_WithNullConfiguration_ReturnsOkWithDefaultCurrency()
		{
			// Arrange
			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns((ConfigurationInstance)null!);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("EUR");
		}

		[Fact]
		public void GetPrimaryCurrency_WithNullSettings_ReturnsOkWithDefaultCurrency()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = null!
			};
			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("EUR");
		}

		[Fact]
		public void GetPrimaryCurrency_WithEmptyPrimaryCurrency_ReturnsOkWithDefaultCurrency()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = ""
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("EUR");
		}

		[Fact]
		public void GetPrimaryCurrency_WithWhitespacePrimaryCurrency_ReturnsOkWithDefaultCurrency()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = "   "
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("EUR");
		}

		[Theory]
		[InlineData("USD")]
		[InlineData("EUR")]
		[InlineData("GBP")]
		[InlineData("JPY")]
		[InlineData("CHF")]
		[InlineData("CAD")]
		public void GetPrimaryCurrency_WithValidCurrencyCodes_ReturnsCorrectCurrency(string currencyCode)
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = currencyCode
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be(currencyCode);
		}

		[Fact]
		public void GetPrimaryCurrency_WhenApplicationSettingsThrowsException_ReturnsInternalServerError()
		{
			// Arrange
			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Throws(new Exception("Configuration access error"));

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().NotBeNull();

			var message = statusCodeResult.Value!.GetType().GetProperty("message")?.GetValue(statusCodeResult.Value)?.ToString();
			var error = statusCodeResult.Value.GetType().GetProperty("error")?.GetValue(statusCodeResult.Value)?.ToString();

			message.Should().Be("Failed to retrieve primary currency");
			error.Should().Be("Configuration access error");
		}

		[Fact]
		public void GetPrimaryCurrency_VerifyResponseStructure()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = "USD"
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			// Verify the response has the expected structure
			var responseType = okResult.Value!.GetType();
			var primaryCurrencyProperty = responseType.GetProperty("PrimaryCurrency");
			primaryCurrencyProperty.Should().NotBeNull();
			primaryCurrencyProperty!.PropertyType.Should().Be<string>();
		}

		[Fact]
		public void GetPrimaryCurrency_WithComplexNestedStructure_HandlesGracefully()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = "CHF"
				}
			};

			// Simulate a more complex scenario where the structure is deeply nested
			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("CHF");
		}

		[Fact]
		public void GetPrimaryCurrency_EnsuresApplicationSettingsIsCalledOnce()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = "USD"
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			_controller.GetPrimaryCurrency();

			// Assert
			_mockApplicationSettings.Verify(x => x.ConfigurationInstance, Times.Once);
		}

		[Fact]
		public void GetPrimaryCurrency_WithNullPrimaryCurrency_ReturnsOkWithDefaultCurrency()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = null!
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("EUR");
		}

		[Fact]
		public void GetPrimaryCurrency_WithTabsAndNewlinesPrimaryCurrency_ReturnsOkWithDefaultCurrency()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = "\t\n\r  "
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be("EUR");
		}

		[Fact]
		public void GetPrimaryCurrency_HttpContext_IsNotNull()
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = "USD"
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			_controller.HttpContext.Should().NotBeNull();
			result.Should().BeOfType<OkObjectResult>();
		}

		[Theory]
		[InlineData("usd", "usd")] // Case sensitivity test
		[InlineData("Eur", "Eur")] // Mixed case test
		[InlineData("123", "123")] // Numeric currency code (edge case)
		[InlineData("CUSTOM", "CUSTOM")] // Custom currency code
		public void GetPrimaryCurrency_PreservesExactCurrencyFormat(string inputCurrency, string expectedCurrency)
		{
			// Arrange
			var configurationInstance = new ConfigurationInstance
			{
				Settings = new Settings()
				{
					DataProviderPreference = "YAHOO",
					PrimaryCurrency = inputCurrency
				}
			};

			_mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			// Act
			var result = _controller.GetPrimaryCurrency();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();

			var primaryCurrency = okResult.Value!.GetType().GetProperty("PrimaryCurrency")?.GetValue(okResult.Value)?.ToString();
			primaryCurrency.Should().Be(expectedCurrency);
		}
	}
}