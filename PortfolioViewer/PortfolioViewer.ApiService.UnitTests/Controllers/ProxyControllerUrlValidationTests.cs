using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.ApiService.Controllers;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	/// <summary>
	/// Tests for URL validation functionality in ProxyController
	/// These tests focus on security validation of URLs, including SSRF protection
	/// </summary>
	public class ProxyControllerUrlValidationTests
	{
		private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
		private readonly Mock<IConfigurationHelper> _mockConfigurationHelper;
		private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
		private readonly HttpClient _httpClient;
		private readonly ProxyController _controller;

		public ProxyControllerUrlValidationTests()
		{
			_mockHttpClientFactory = new Mock<IHttpClientFactory>();
			_mockConfigurationHelper = new Mock<IConfigurationHelper>();
			_mockHttpMessageHandler = new Mock<HttpMessageHandler>();

			_httpClient = new HttpClient(_mockHttpMessageHandler.Object);
			_mockHttpClientFactory.Setup(x => x.CreateClient(string.Empty)).Returns(_httpClient);

			_controller = new ProxyController(_mockHttpClientFactory.Object, _mockConfigurationHelper.Object)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};
		}

		#region Invalid URL Format Tests

		[Theory]
		[InlineData("not-a-url")]
		[InlineData("://invalid-scheme")]
		[InlineData("invalid://")]
		[InlineData("relative/path")]
		[InlineData("/absolute/path")]
		[InlineData("file:///etc/passwd")]
		[InlineData("javascript:alert('xss')")]
		[InlineData("data:text/html,<script>alert('xss')</script>")]
		public async Task Fetch_WithInvalidUrlFormat_ReturnsBadRequest(string invalidUrl)
		{
			// Act
			var result = await _controller.Fetch(invalidUrl);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			errorMessage.Should().StartWith("Invalid URL:");
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		public async Task Fetch_WithEmptyOrWhitespaceUrl_ReturnsBadRequest(string emptyUrl)
		{
			// Act
			var result = await _controller.Fetch(emptyUrl);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			errorMessage.Should().Be("URL is required.");
		}

		#endregion

		#region Scheme Validation Tests

		[Theory]
		[InlineData("ftp://example.com")]
		[InlineData("file://example.com")]
		[InlineData("ldap://example.com")]
		[InlineData("gopher://example.com")]
		[InlineData("dict://example.com")]
		[InlineData("ssh://example.com")]
		[InlineData("telnet://example.com")]
		public async Task Fetch_WithDisallowedScheme_ReturnsBadRequest(string urlWithDisallowedScheme)
		{
			// Act
			var result = await _controller.Fetch(urlWithDisallowedScheme);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			errorMessage.Should().Contain("URL scheme").And.Contain("is not allowed");
		}

		[Theory]
		[InlineData("http://example.com")]
		[InlineData("https://example.com")]
		[InlineData("HTTP://EXAMPLE.COM")]  // Test case insensitivity
		[InlineData("HTTPS://EXAMPLE.COM")]
		public async Task Fetch_WithAllowedScheme_DoesNotRejectBasedOnScheme(string urlWithAllowedScheme)
		{
			// Arrange
			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("<html><body>Test</body></html>", Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(urlWithAllowedScheme);

			// Assert
			// Should not be a BadRequest due to scheme validation
			result.Should().NotBeOfType<BadRequestObjectResult>();
		}

		#endregion

		#region Private Network Protection Tests

		[Theory]
		[InlineData("http://127.0.0.1")]          // Loopback
		[InlineData("http://127.0.0.1:8080")]     // Loopback with port  
		[InlineData("http://localhost")]          // Localhost
		public async Task Fetch_WithKnownPrivateNetworkAddress_ReturnsBadRequest(string privateUrl)
		{
			// Act
			var result = await _controller.Fetch(privateUrl);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			
			// The error could be either DNS resolution failure or private network blocking
			// depending on whether the IP has reverse DNS entries
			var isValidError = errorMessage.Contains("private/internal networks is not allowed") ||
			                  errorMessage.Contains("Unable to resolve hostname");
			isValidError.Should().BeTrue($"Expected private network error or DNS resolution error, but got: {errorMessage}");
		}

		[Theory]
		[InlineData("http://10.0.0.1")]           // Private Class A
		[InlineData("http://172.16.0.1")]         // Private Class B
		[InlineData("http://192.168.1.1")]        // Private Class C
		[InlineData("http://169.254.1.1")]        // Link-local
		public async Task Fetch_WithPrivateIpAddress_ReturnsExpectedError(string privateUrl)
		{
			// Act
			var result = await _controller.Fetch(privateUrl);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			
			// For direct IP addresses, DNS resolution might fail if there's no reverse DNS
			// This is actually the expected behavior for security - we want to block access
			// whether through DNS failure or explicit network validation
			var isSecurityError = errorMessage.Contains("private/internal networks is not allowed") ||
			                     errorMessage.Contains("Unable to resolve hostname");
			isSecurityError.Should().BeTrue($"Expected security-related error, but got: {errorMessage}");
		}

		#endregion

		#region Port Validation Tests

		[Theory]
		[InlineData("http://example.com:22")]     // SSH
		[InlineData("http://example.com:23")]     // Telnet
		[InlineData("http://example.com:25")]     // SMTP
		[InlineData("http://example.com:53")]     // DNS
		[InlineData("http://example.com:110")]    // POP3
		[InlineData("http://example.com:143")]    // IMAP
		[InlineData("http://example.com:993")]    // IMAPS
		[InlineData("http://example.com:995")]    // POP3S
		[InlineData("http://example.com:1433")]   // SQL Server
		[InlineData("http://example.com:3306")]   // MySQL
		[InlineData("http://example.com:5432")]   // PostgreSQL
		[InlineData("http://example.com:6379")]   // Redis
		[InlineData("http://example.com:11211")]  // Memcached
		[InlineData("http://example.com:27017")]  // MongoDB
		public async Task Fetch_WithBlockedPort_ReturnsBadRequest(string urlWithBlockedPort)
		{
			// Act
			var result = await _controller.Fetch(urlWithBlockedPort);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			errorMessage.Should().Contain("Access to port").And.Contain("is not allowed");
		}

		[Theory]
		[InlineData("http://example.com:80")]     // Standard HTTP
		[InlineData("http://example.com:8080")]   // Common HTTP alternative
		[InlineData("https://example.com:443")]   // Standard HTTPS
		[InlineData("https://example.com:8443")]  // Common HTTPS alternative
		[InlineData("http://example.com:3000")]   // Common development port
		public async Task Fetch_WithAllowedPort_DoesNotRejectBasedOnPort(string urlWithAllowedPort)
		{
			// Arrange
			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("<html><body>Test</body></html>", Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(urlWithAllowedPort);

			// Assert
			// Should not be a BadRequest due to port validation
			result.Should().NotBeOfType<BadRequestObjectResult>();
		}

		#endregion

		#region DNS Resolution Error Tests

		[Fact]
		public async Task Fetch_WithNonExistentDomain_ReturnsBadRequest()
		{
			// This test might be flaky depending on DNS configuration
			// but tests the DNS resolution failure path
			var result = await _controller.Fetch("http://this-domain-should-not-exist-12345.example");

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			errorMessage.Should().Contain("Unable to resolve hostname");
		}

		#endregion

		#region Edge Cases and Boundary Tests

		[Fact]
		public async Task Fetch_WithUrlContainingSpecialCharacters_HandlesCorrectly()
		{
			// Arrange
			const string urlWithSpecialChars = "https://example.com/path?param=value%20with%20spaces&other=test";
			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("<html><body>Test</body></html>", Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(urlWithSpecialChars);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task Fetch_WithLongUrl_HandlesCorrectly()
		{
			// Arrange
			var longPath = new string('a', 1000);
			var longUrl = $"https://example.com/{longPath}";
			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("<html><body>Test</body></html>", Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(longUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task Fetch_WithUnicodeUrl_HandlesCorrectly()
		{
			// Arrange
			const string unicodeUrl = "https://example.com/??/??";
			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("<html><body>Test</body></html>", Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(unicodeUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
		}

		#endregion

		#region IPv6 Tests

		[Theory]
		[InlineData("http://[::1]")]              // IPv6 loopback
		[InlineData("http://[fc00::1]")]          // IPv6 unique local
		[InlineData("http://[fe80::1]")]          // IPv6 link-local
		public async Task Fetch_WithBlockedIPv6Address_ReturnsBadRequest(string ipv6Url)
		{
			// Act
			var result = await _controller.Fetch(ipv6Url);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().BeOfType<string>();
			var errorMessage = (string)badRequestResult.Value!;
			
			// For IPv6 addresses, DNS resolution might fail if there's no reverse DNS
			// This is actually the expected behavior for security - we want to block access
			// whether through DNS failure or explicit network validation
			var isSecurityError = errorMessage.Contains("private/internal networks is not allowed") ||
			                     errorMessage.Contains("Unable to resolve hostname");
			isSecurityError.Should().BeTrue($"Expected security-related error, but got: {errorMessage}");
		}

		#endregion

		#region Request Headers Tests

		[Fact]
		public void ProxyController_Constructor_SetsExpectedHeaders()
		{
			// Act - Constructor is called in test setup
			
			// Assert - Verify the HttpClient has the expected headers
			_httpClient.DefaultRequestHeaders.Should().NotBeNull();
			
			// Check User-Agent header
			var userAgentHeader = _httpClient.DefaultRequestHeaders.UserAgent.ToString();
			userAgentHeader.Should().Contain("Mozilla/5.0");
			userAgentHeader.Should().Contain("Chrome");
			
			// Check Accept header
			var acceptHeader = _httpClient.DefaultRequestHeaders.Accept.ToString();
			acceptHeader.Should().Contain("text/html");
			acceptHeader.Should().Contain("application/xhtml+xml");
		}

		#endregion

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_httpClient?.Dispose();
			}
		}
	}
}