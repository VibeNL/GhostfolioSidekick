using System.Net;
using GhostfolioSidekick.ExternalDataProvider.Citi;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace GhostfolioSidekick.ExternalDataProvider.UnitTests.Citi
{
	public class CitiAdrRatioProviderTests
	{
		[Fact]
		public async Task GetSharesPerReceiptAsync_UsIsinWithValidRatio_ReturnsRatio()
		{
			// Arrange
			var content = "<td>Ratio (ORD:DRS)&nbsp;</td><td class=\"mwRight\">25      :1       </td>";
			var provider = CreateProvider(HttpStatusCode.OK, content);

			// Act
			var result = await provider.GetSharesPerReceiptAsync("US7960508882");

			// Assert
			Assert.Equal(25m, result);
		}

		[Fact]
		public async Task GetSharesPerReceiptAsync_NonUsIsin_ReturnsNullWithoutCallingHttp()
		{
			// Arrange
			var provider = CreateProvider(HttpStatusCode.OK, "Ratio (ORD:DRS) 25:1", out var handlerMock);

			// Act
			var result = await provider.GetSharesPerReceiptAsync("KR7005930003");

			// Assert
			Assert.Null(result);
			handlerMock.Protected()
				.Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
		}

		[Fact]
		public async Task GetSharesPerReceiptAsync_HttpFailure_ReturnsNull()
		{
			// Arrange
			var provider = CreateProvider(HttpStatusCode.InternalServerError, string.Empty);

			// Act
			var result = await provider.GetSharesPerReceiptAsync("US7960508882");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public async Task GetSharesPerReceiptAsync_NoRatioInResponse_ReturnsNull()
		{
			// Arrange
			var provider = CreateProvider(HttpStatusCode.OK, "no ratio here");

			// Act
			var result = await provider.GetSharesPerReceiptAsync("US7960508882");

			// Assert
			Assert.Null(result);
		}

		private static CitiAdrRatioProvider CreateProvider(HttpStatusCode statusCode, string content)
		{
			return CreateProvider(statusCode, content, out _);
		}

		private static CitiAdrRatioProvider CreateProvider(HttpStatusCode statusCode, string content, out Mock<HttpMessageHandler> handlerMock)
		{
			var loggerMock = new Mock<ILogger<CitiAdrRatioProvider>>();
			handlerMock = new Mock<HttpMessageHandler>();
			handlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode, Content = new StringContent(content) });

			var httpClient = new HttpClient(handlerMock.Object);
			return new CitiAdrRatioProvider(loggerMock.Object, httpClient);
		}
	}
}
