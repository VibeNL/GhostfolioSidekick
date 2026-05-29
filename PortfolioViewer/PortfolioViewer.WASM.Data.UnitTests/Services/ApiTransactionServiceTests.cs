using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Services
{
	public class ApiTransactionServiceTests
	{
		private readonly Mock<HttpMessageHandler> _handlerMock;
		private readonly HttpClient _httpClient;
		private readonly ApiTransactionService _service;

		public ApiTransactionServiceTests()
		{
			_handlerMock = new Mock<HttpMessageHandler>();
			_httpClient = new HttpClient(_handlerMock.Object)
			{
				BaseAddress = new Uri("https://test.api/")
			};
			_service = new ApiTransactionService(_httpClient);
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
		public async Task GetTransactionsPaginatedAsync_ReturnsEmptyResult_WhenApiReturnsNull()
		{
			SetupResponse(HttpStatusCode.OK, (object?)null);

			var result = await _service.GetTransactionsPaginatedAsync(
				new TransactionQueryParameters(), CancellationToken.None);

			result.Should().NotBeNull();
			result.Transactions.Should().BeEmpty();
			result.TotalCount.Should().Be(0);
		}

		[Fact]
		public async Task GetTransactionsPaginatedAsync_ReturnsMappedTransactions()
		{
			var payload = new
			{
				Transactions = new[]
				{
					new
					{
						Id = 1L,
						Date = DateTime.UtcNow,
						Type = "BUY",
						Symbol = "AAPL",
						Name = "Apple",
						Description = "",
						TransactionId = "T1",
						AccountName = "Acc1",
						Quantity = (decimal?)10m,
						UnitPriceAmount = (decimal?)150m,
						UnitPriceCurrency = "USD",
						AmountValue = (decimal?)null,
						AmountCurrency = "",
						TotalValueAmount = (decimal?)1500m,
						TotalValueCurrency = "USD",
						FeeAmount = (decimal?)null,
						FeeCurrency = "",
						TaxAmount = (decimal?)null,
						TaxCurrency = ""
					}
				},
				TotalCount = 1,
				PageNumber = 1,
				PageSize = 25,
				TransactionTypeBreakdown = new Dictionary<string, int> { ["BUY"] = 1 },
				AccountBreakdown = new Dictionary<string, int> { ["Acc1"] = 1 }
			};
			SetupResponse(HttpStatusCode.OK, payload);

			var result = await _service.GetTransactionsPaginatedAsync(
				new TransactionQueryParameters(), CancellationToken.None);

			result.TotalCount.Should().Be(1);
			result.Transactions.Should().HaveCount(1);
			result.Transactions[0].Symbol.Should().Be("AAPL");
			result.Transactions[0].TotalValue!.Amount.Should().Be(1500m);
		}

		[Fact]
		public async Task GetTransactionTypesAsync_ReturnsEmpty_WhenApiReturnsEmptyArray()
		{
			SetupResponse(HttpStatusCode.OK, Array.Empty<string>());

			var result = await _service.GetTransactionTypesAsync(CancellationToken.None);

			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetTransactionTypesAsync_ReturnsList_WhenApiReturnsValues()
		{
			SetupResponse(HttpStatusCode.OK, new[] { "BUY", "SELL", "DIVIDEND" });

			var result = await _service.GetTransactionTypesAsync(CancellationToken.None);

			result.Should().Contain("BUY").And.Contain("SELL").And.Contain("DIVIDEND");
		}
	}
}
