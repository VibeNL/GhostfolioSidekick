using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
	public class ServerConfigurationServiceTests
	{
		private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
		private readonly HttpClient _httpClient;
		private readonly ServerConfigurationService _serverConfigurationService;

		public ServerConfigurationServiceTests()
		{
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
			_httpClient = new HttpClient(_httpMessageHandlerMock.Object)
			{
				BaseAddress = new Uri("https://test.com/")
			};
			_serverConfigurationService = new ServerConfigurationService(_httpClient);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiReturnsEUR_ReturnsEUR()
		{
			// Arrange
			var responseContent = JsonSerializer.Serialize(new { PrimaryCurrency = "EUR" });
			SetupHttpResponse(HttpStatusCode.OK, responseContent);

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiReturnsUSD_ReturnsUSD()
		{
			// Arrange
			var responseContent = JsonSerializer.Serialize(new { PrimaryCurrency = "USD" });
			SetupHttpResponse(HttpStatusCode.OK, responseContent);

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Should().Be(Currency.USD);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiReturnsGBP_ReturnsGBP()
		{
			// Arrange
			var responseContent = JsonSerializer.Serialize(new { PrimaryCurrency = "GBP" });
			SetupHttpResponse(HttpStatusCode.OK, responseContent);

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Should().Be(Currency.GBP);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiReturnsUnknownCurrency_CreatesNewCurrency()
		{
			// Arrange
			var responseContent = JsonSerializer.Serialize(new { PrimaryCurrency = "JPY" });
			SetupHttpResponse(HttpStatusCode.OK, responseContent);

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Symbol.Should().Be("JPY");
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiReturnsEmptyString_ReturnsEUR()
		{
			// Arrange
			var responseContent = JsonSerializer.Serialize(new { PrimaryCurrency = "" });
			SetupHttpResponse(HttpStatusCode.OK, responseContent);

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiReturnsNull_ReturnsEUR()
		{
			// Arrange
			var responseContent = JsonSerializer.Serialize(new { PrimaryCurrency = (string?)null });
			SetupHttpResponse(HttpStatusCode.OK, responseContent);

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiCallFails_ReturnsEUR()
		{
			// Arrange
			SetupHttpResponse(HttpStatusCode.InternalServerError, "Error");

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenApiReturnsInvalidJson_ReturnsEUR()
		{
			// Arrange
			SetupHttpResponse(HttpStatusCode.OK, "invalid json");

			// Act
			var result = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result.Should().Be(Currency.EUR);
		}

		[Fact]
		public async Task GetPrimaryCurrencyAsync_WhenCalledMultipleTimes_CachesResult()
		{
			// Arrange
			var responseContent = JsonSerializer.Serialize(new { PrimaryCurrency = "USD" });
			SetupHttpResponse(HttpStatusCode.OK, responseContent);

			// Act
			var result1 = await _serverConfigurationService.GetPrimaryCurrencyAsync();
			var result2 = await _serverConfigurationService.GetPrimaryCurrencyAsync();
			var result3 = await _serverConfigurationService.GetPrimaryCurrencyAsync();

			// Assert
			result1.Should().Be(Currency.USD);
			result2.Should().Be(Currency.USD);
			result3.Should().Be(Currency.USD);
			result1.Should().Be(result2);
			result2.Should().Be(result3);

			// Verify the HTTP call was made only once (due to caching)
			_httpMessageHandlerMock.Protected()
				.Verify("SendAsync", Times.Once(),
					ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/configuration/primary-currency")),
					ItExpr.IsAny<CancellationToken>());
		}

		private void SetupHttpResponse(HttpStatusCode statusCode, string content)
		{
			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync",
					ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/configuration/primary-currency")),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = statusCode,
					Content = new StringContent(content)
				});
		}
	}
}