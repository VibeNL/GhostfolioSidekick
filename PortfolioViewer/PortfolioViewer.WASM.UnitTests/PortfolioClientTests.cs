using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using FluentAssertions;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Xunit;

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
	}
}
