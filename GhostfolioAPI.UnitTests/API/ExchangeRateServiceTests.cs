using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RestSharp;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class ExchangeRateServiceTests : BaseAPITests
	{
		private readonly Mock<ILogger<ExchangeRateService>> loggerMock;
		private readonly ExchangeRateService exchangeRateService;
		private readonly string exchangeUrl = "api/v1/exchange-rate";

		public ExchangeRateServiceTests()
		{
			loggerMock = new Mock<ILogger<ExchangeRateService>>();

			exchangeRateService = new ExchangeRateService(
				restCall,
				new MemoryCache(new MemoryCacheOptions()),
				loggerMock.Object);
		}

		[Fact]
		public async Task GetConversionRate_Success()
		{
			// Arrange
			var sourceCurrency = Currency.USD;
			var targetCurrency = Currency.EUR;
			var dateTime = DateTime.Now;

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(exchangeUrl)), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, "{\"marketPrice\": \"0.85\"}"));

			// Act
			var result = await exchangeRateService.GetConversionRate(sourceCurrency, targetCurrency, dateTime);

			// Assert
			result.Should().Be(0.85m);
		}

		[Fact]
		public async Task GetConversionRate_Failed()
		{
			// Arrange
			var sourceCurrency = Currency.USD;
			var targetCurrency = Currency.EUR;
			var dateTime = DateTime.Now;

			restClient
				.Setup(x => x.ExecuteAsync(It.Is<RestRequest>(x => x.Resource.Contains(exchangeUrl)), default))
				.ReturnsAsync(CreateResponse(System.Net.HttpStatusCode.OK, null));


			// Act
			var result = await exchangeRateService.GetConversionRate(sourceCurrency, targetCurrency, dateTime);

			// Assert
			result.Should().Be(1);
		}

		[Fact]
		public async Task GetConversionRate_SameCurrency()
		{
			// Arrange
			var sourceCurrency = Currency.USD;
			var targetCurrency = Currency.USD;
			var dateTime = DateTime.Now;

			// Act
			var result = await exchangeRateService.GetConversionRate(sourceCurrency, targetCurrency, dateTime);

			// Assert
			result.Should().Be(1);
		}
	}
}
