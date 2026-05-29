using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class ApiAccountDataServiceTests
	{
		private readonly Mock<HttpMessageHandler> _handlerMock;
		private readonly HttpClient _httpClient;
		private readonly Mock<IServerConfigurationService> _serverConfigMock;
		private readonly ApiAccountDataService _service;

		public ApiAccountDataServiceTests()
		{
			_handlerMock = new Mock<HttpMessageHandler>();
			_httpClient = new HttpClient(_handlerMock.Object)
			{
				BaseAddress = new Uri("https://test.api/")
			};
			_serverConfigMock = new Mock<IServerConfigurationService>();
			_serverConfigMock.Setup(x => x.PrimaryCurrency).Returns(Currency.USD);
			_service = new ApiAccountDataService(_httpClient, _serverConfigMock.Object);
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
		public async Task GetAccountInfo_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetAccountInfo();

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetAccountByIdAsync_ReturnsNull_WhenApiReturns404()
		{
			_handlerMock
				.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound });

			var result = await _service.GetAccountByIdAsync(99);

			result.Should().BeNull();
		}

		[Fact]
		public async Task GetMinDateAsync_ParsesReturnedDateString()
		{
			SetupResponse(HttpStatusCode.OK, "2023-01-01");

			var result = await _service.GetMinDateAsync(CancellationToken.None);

			result.Should().Be(new DateOnly(2023, 1, 1));
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<string>());

			var result = await _service.GetSymbolProfilesAsync(null, CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetSymbolProfilesAsync_WithAccountFilter_IncludesFilterParam()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<string>());

			await _service.GetSymbolProfilesAsync(7, CancellationToken.None);

			_handlerMock.Protected().Verify(
				"SendAsync",
				Times.Once(),
				ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.Query.Contains("accountFilter=7")),
				ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public async Task GetAccountValueHistoryAsync_ReturnsNull_WhenApiReturnsNull()
		{
			SetupResponse(HttpStatusCode.OK, (object?)null);

			var result = await _service.GetAccountValueHistoryAsync(
				DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
				DateOnly.FromDateTime(DateTime.Today),
				CancellationToken.None);

			result.Should().BeNull();
		}

		[Fact]
		public async Task GetTaxReportAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetTaxReportAsync(CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}
	}
}
