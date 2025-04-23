using System.Text.Json;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using Moq;
using GhostfolioSidekick.PortfolioViewer.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
	public class PortfolioClientTests
	{
		private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
		private readonly Mock<DatabaseContext> _databaseContextMock;
		private readonly HttpClient _httpClient;
		private readonly PortfolioClient _portfolioClient;

		public PortfolioClientTests()
		{
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
			_databaseContextMock = new Mock<DatabaseContext>();
			_httpClient = new HttpClient(_httpMessageHandlerMock.Object);
			_portfolioClient = new PortfolioClient(_httpClient, _databaseContextMock.Object);
		}

		[Fact]
		public void DeserializeData_ShouldReturnDeserializedData()
		{
			// Arrange
			var jsonData = JsonSerializer.Serialize(new List<Dictionary<string, object>>
			{
				new Dictionary<string, object> { { "Column1", "Value1" }, { "Column2", 123 } }
			});

			// Act
			var result = PortfolioClient.DeserializeData(jsonData);

			// Assert
			Assert.Single(result);
			Assert.Equal("Value1", result[0]["Column1"].ToString());
			Assert.Equal("123", result[0]["Column2"].ToString());
		}

		[Fact]
		public async Task GetValueOverTimeData_ShouldReturnData()
		{
			// Arrange
			var mockData = new List<MarketData>
			{
				new MarketData { Date = new DateOnly(2023, 1, 1), Close = new Money { Amount = 100, Currency = new Currency { Code = "USD" } } }
			};
			var jsonData = JsonSerializer.Serialize(mockData);
			_httpMessageHandlerMock.Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent(jsonData) });

			// Act
			var result = await _portfolioClient.GetValueOverTimeData();

			// Assert
			Assert.Single(result);
			Assert.Equal(100, result[0].Close.Amount);
		}

		[Fact]
		public async Task GetProfitOverTimeData_ShouldReturnData()
		{
			// Arrange
			var mockData = new List<MarketData>
			{
				new MarketData { Date = new DateOnly(2023, 1, 1), Close = new Money { Amount = 200, Currency = new Currency { Code = "USD" } } }
			};
			var jsonData = JsonSerializer.Serialize(mockData);
			_httpMessageHandlerMock.Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent(jsonData) });

			// Act
			var result = await _portfolioClient.GetProfitOverTimeData();

			// Assert
			Assert.Single(result);
			Assert.Equal(200, result[0].Close.Amount);
		}

		[Fact]
		public async Task GetDividendsPerMonthData_ShouldReturnData()
		{
			// Arrange
			var mockData = new List<MarketData>
			{
				new MarketData { Date = new DateOnly(2023, 1, 1), Close = new Money { Amount = 300, Currency = new Currency { Code = "USD" } } }
			};
			var jsonData = JsonSerializer.Serialize(mockData);
			_httpMessageHandlerMock.Setup(handler => handler.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK, Content = new StringContent(jsonData) });

			// Act
			var result = await _portfolioClient.GetDividendsPerMonthData();

			// Assert
			Assert.Single(result);
			Assert.Equal(300, result[0].Close.Amount);
		}
	}
}
