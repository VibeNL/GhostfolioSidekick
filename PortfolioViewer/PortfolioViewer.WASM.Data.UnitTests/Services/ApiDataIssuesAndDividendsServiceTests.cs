using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class ApiDataIssuesServiceTests
	{
		private readonly Mock<HttpMessageHandler> _handlerMock;
		private readonly HttpClient _httpClient;
		private readonly ApiDataIssuesService _service;

		public ApiDataIssuesServiceTests()
		{
			_handlerMock = new Mock<HttpMessageHandler>();
			_httpClient = new HttpClient(_handlerMock.Object)
			{
				BaseAddress = new Uri("https://test.api/")
			};
			_service = new ApiDataIssuesService(_httpClient);
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
		public async Task GetActivitiesWithoutHoldingsAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_ReturnsEmpty_WhenApiReturnsNull()
		{
			SetupResponse(HttpStatusCode.OK, (object?)null);

			var result = await _service.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetActivitiesWithoutHoldingsAsync_MapsSeverityAndDescription()
		{
			var data = new[]
			{
				new
				{
					Id = 1L,
					IssueType = "MissingHolding",
					Description = "No holding found for AAPL",
					Date = DateTime.UtcNow,
					AccountName = "Broker",
					ActivityType = "BUY",
					Symbol = "AAPL",
					SymbolIdentifiers = (string?)null,
					Quantity = (decimal?)10m,
					UnitPriceAmount = (decimal?)null,
					UnitPriceCurrency = "",
					AmountValue = (decimal?)null,
					AmountCurrency = "",
					TransactionId = "T1",
					ActivityDescription = (string?)null,
					Severity = "Error"
				}
			};
			SetupResponse(HttpStatusCode.OK, data);

			var result = await _service.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			result.Should().HaveCount(1);
			result[0].IssueType.Should().Be("MissingHolding");
			result[0].Severity.Should().Be("Error");
			result[0].Symbol.Should().Be("AAPL");
		}
	}

	public class ApiUpcomingDividendsServiceTests
	{
		private readonly Mock<HttpMessageHandler> _handlerMock;
		private readonly HttpClient _httpClient;
		private readonly ApiUpcomingDividendsService _service;

		public ApiUpcomingDividendsServiceTests()
		{
			_handlerMock = new Mock<HttpMessageHandler>();
			_httpClient = new HttpClient(_handlerMock.Object)
			{
				BaseAddress = new Uri("https://test.api/")
			};
			_service = new ApiUpcomingDividendsService(_httpClient);
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
		public async Task GetUpcomingDividendsAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<object>());

			var result = await _service.GetUpcomingDividendsAsync();

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetUpcomingDividendsAsync_ReturnsEmpty_WhenApiReturnsNull()
		{
			SetupResponse(HttpStatusCode.OK, (object?)null);

			var result = await _service.GetUpcomingDividendsAsync();

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetUpcomingDividendsAsync_MapsDividendFields()
		{
			var data = new[]
			{
				new
				{
					Symbol = "MSFT",
					CompanyName = "Microsoft",
					ExDate = "2024-03-15",
					PaymentDate = "2024-04-01",
					Amount = 0.75m,
					Currency = "USD",
					DividendPerShare = 0.75m,
					AmountPrimaryCurrency = (decimal?)0.75m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = (decimal?)0.75m,
					Quantity = 100m,
					IsPredicted = false
				}
			};
			SetupResponse(HttpStatusCode.OK, data);

			var result = await _service.GetUpcomingDividendsAsync();

			result.Should().HaveCount(1);
			result[0].Symbol.Should().Be("MSFT");
			result[0].Amount.Should().Be(0.75m);
			result[0].Quantity.Should().Be(100m);
			result[0].IsPredicted.Should().BeFalse();
		}
	}
}
