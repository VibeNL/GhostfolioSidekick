using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class ApiHoldingsDataServiceTests
	{
		private readonly Mock<HttpMessageHandler> _handlerMock;
		private readonly HttpClient _httpClient;
		private readonly Mock<IServerConfigurationService> _serverConfigMock;
		private readonly ApiHoldingsDataService _service;

		public ApiHoldingsDataServiceTests()
		{
			_handlerMock = new Mock<HttpMessageHandler>();
			_httpClient = new HttpClient(_handlerMock.Object)
			{
				BaseAddress = new Uri("https://test.api/")
			};
			_serverConfigMock = new Mock<IServerConfigurationService>();
			_serverConfigMock.Setup(x => x.PrimaryCurrency).Returns(GhostfolioSidekick.Model.Currency.USD);

			_service = new ApiHoldingsDataService(_httpClient, _serverConfigMock.Object);
		}

		private void SetupResponse(HttpStatusCode statusCode, object? body)
		{
			var json = body == null ? "null" : JsonSerializer.Serialize(body);
			_handlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = statusCode,
					Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
				});
		}

		[Fact]
		public async Task GetHoldingsAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetHoldingsAsync(CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetHoldingsAsync_WithAccountId_UsesAccountEndpoint()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetHoldingsAsync(accountId: 42, CancellationToken.None);

			result.Should().NotBeNull();
			_handlerMock.Protected().Verify(
				"SendAsync",
				Times.Once(),
				ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("account/42")),
				ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public async Task GetHoldingAsync_ReturnsNull_WhenApiReturns404()
		{
			_handlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

			var result = await _service.GetHoldingAsync("AAPL", CancellationToken.None);

			result.Should().BeNull();
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetHoldingPriceHistoryAsync(
				"AAPL",
				DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Today),
				CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetPortfolioValueHistoryAsync(
				DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Today),
				accountId: null,
				CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetPortfolioValueHistoryAsync_WithAccountId_IncludesAccountParam()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			await _service.GetPortfolioValueHistoryAsync(
				DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Today),
				accountId: 5,
				CancellationToken.None);

			_handlerMock.Protected().Verify(
				"SendAsync",
				Times.Once(),
				ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.Query.Contains("accountId=5")),
				ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public async Task GetHoldingPriceHistoryAsync_MapsDateCorrectly()
		{
			var data = new[]
			{
				new { Date = "2024-01-15", Price = 150.0m, AveragePrice = 140.0m }
			};
			SetupResponse(HttpStatusCode.OK, data);

			var result = await _service.GetHoldingPriceHistoryAsync(
				"AAPL",
				new DateOnly(2024, 1, 1),
				new DateOnly(2024, 1, 31),
				CancellationToken.None);

			result.Should().HaveCount(1);
			result[0].Date.Should().Be(new DateOnly(2024, 1, 15));
			result[0].Price.Should().Be(150.0m);
			result[0].AveragePrice.Should().Be(140.0m);
		}
	}
}
