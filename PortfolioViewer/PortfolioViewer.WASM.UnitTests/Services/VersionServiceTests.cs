using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
	public class VersionServiceTests
	{
		private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
		private readonly HttpClient _httpClient;
		private readonly VersionService _versionService;

		public VersionServiceTests()
		{
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
			_httpClient = new HttpClient(_httpMessageHandlerMock.Object)
			{
				BaseAddress = new Uri("https://test.com/")
			};
			_versionService = new VersionService(_httpClient);
		}

		[Fact]
		public void ClientVersion_ReturnsNonEmptyString()
		{
			_versionService.ClientVersion.Should().NotBeNullOrEmpty();
		}

		[Fact]
		public async Task GetServerVersionAsync_WhenApiReturnsVersion_ReturnsVersion()
		{
			SetupVersionResponse(HttpStatusCode.OK, "1.0.0");

			var result = await _versionService.GetServerVersionAsync();

			result.Should().Be("1.0.0");
		}

		[Fact]
		public async Task GetServerVersionAsync_WhenApiFails_ReturnsNull()
		{
			SetupVersionResponse(HttpStatusCode.InternalServerError, "error");

			var result = await _versionService.GetServerVersionAsync();

			result.Should().BeNull();
		}

		[Fact]
		public async Task GetServerVersionAsync_WhenExceptionThrown_ReturnsNull()
		{
			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync",
					ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/version")),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException("Network error"));

			var result = await _versionService.GetServerVersionAsync();

			result.Should().BeNull();
		}

		[Fact]
		public async Task GetServerVersionAsync_ThenApiFails_ReturnsCachedVersion()
		{
			SetupVersionResponse(HttpStatusCode.OK, "2.0.0");

			var result1 = await _versionService.GetServerVersionAsync();
			result1.Should().Be("2.0.0");

			SetupVersionResponse(HttpStatusCode.InternalServerError, "error");

			var result2 = await _versionService.GetServerVersionAsync();
			result2.Should().Be("2.0.0");
		}

		[Fact]
		public async Task IsUpdateAvailableAsync_WhenServerVersionDiffers_ReturnsTrue()
		{
			SetupVersionResponse(HttpStatusCode.OK, "99.99.99");

			var result = await _versionService.IsUpdateAvailableAsync();

			result.Should().BeTrue();
		}

		[Fact]
		public async Task IsUpdateAvailableAsync_WhenServerVersionMatches_ReturnsFalse()
		{
			SetupVersionResponse(HttpStatusCode.OK, _versionService.ClientVersion);

			var result = await _versionService.IsUpdateAvailableAsync();

			result.Should().BeFalse();
		}

		[Fact]
		public async Task IsUpdateAvailableAsync_WhenApiFails_ReturnsFalse()
		{
			SetupVersionResponse(HttpStatusCode.InternalServerError, "error");

			var result = await _versionService.IsUpdateAvailableAsync();

			result.Should().BeFalse();
		}

		private void SetupVersionResponse(HttpStatusCode statusCode, string version)
		{
			var responseContent = JsonSerializer.Serialize(new { Version = version });
			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync",
					ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api/version")),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = statusCode,
					Content = new StringContent(responseContent)
				});
		}
	}
}
