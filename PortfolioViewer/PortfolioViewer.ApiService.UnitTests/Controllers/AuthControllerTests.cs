using AwesomeAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.PortfolioViewer.ApiService.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	public class AuthControllerTests
	{
		private readonly Mock<IApplicationSettings> _mockApplicationSettings;
		private readonly AuthController _controller;

		public AuthControllerTests()
		{
			_mockApplicationSettings = new Mock<IApplicationSettings>();
			_controller = new AuthController(_mockApplicationSettings.Object)
			{
				// Setup HttpContext to avoid null reference exceptions
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};
		}

		[Fact]
		public void ValidateToken_WithMissingAuthorizationHeader_ReturnsUnauthorized()
		{
			// Arrange
			// No Authorization header is set

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<UnauthorizedObjectResult>();
			var unauthorizedResult = (UnauthorizedObjectResult)result;
			unauthorizedResult.Value.Should().NotBeNull();
			
			var message = unauthorizedResult.Value!.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value)?.ToString();
			message.Should().Be("Invalid authorization header format");
		}

		[Fact]
		public void ValidateToken_WithEmptyAuthorizationHeader_ReturnsUnauthorized()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "";

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<UnauthorizedObjectResult>();
			var unauthorizedResult = (UnauthorizedObjectResult)result;
			unauthorizedResult.Value.Should().NotBeNull();
			
			var message = unauthorizedResult.Value!.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value)?.ToString();
			message.Should().Be("Invalid authorization header format");
		}

		[Fact]
		public void ValidateToken_WithInvalidAuthorizationFormat_ReturnsUnauthorized()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "InvalidFormat token123";

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<UnauthorizedObjectResult>();
			var unauthorizedResult = (UnauthorizedObjectResult)result;
			unauthorizedResult.Value.Should().NotBeNull();
			
			var message = unauthorizedResult.Value!.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value)?.ToString();
			message.Should().Be("Invalid authorization header format");
		}

		[Fact]
		public void ValidateToken_WithEmptyToken_ReturnsUnauthorized()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "Bearer ";

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<UnauthorizedObjectResult>();
			var unauthorizedResult = (UnauthorizedObjectResult)result;
			unauthorizedResult.Value.Should().NotBeNull();
			
			var message = unauthorizedResult.Value!.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value)?.ToString();
			message.Should().Be("Token is required");
		}

		[Fact]
		public void ValidateToken_WithOnlyWhitespaceToken_ReturnsUnauthorized()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "Bearer    ";

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<UnauthorizedObjectResult>();
			var unauthorizedResult = (UnauthorizedObjectResult)result;
			unauthorizedResult.Value.Should().NotBeNull();
			
			var message = unauthorizedResult.Value!.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value)?.ToString();
			message.Should().Be("Token is required");
		}

		[Fact]
		public void ValidateToken_WithNullConfiguredToken_ReturnsInternalServerError()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "Bearer validtoken123";
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Returns(string.Empty);

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().NotBeNull();
			
			var message = statusCodeResult.Value!.GetType().GetProperty("message")?.GetValue(statusCodeResult.Value)?.ToString();
			message.Should().Be("Server configuration error: No access token configured");
		}

		[Fact]
		public void ValidateToken_WithEmptyConfiguredToken_ReturnsInternalServerError()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "Bearer validtoken123";
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Returns("");

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().NotBeNull();
			
			var message = statusCodeResult.Value!.GetType().GetProperty("message")?.GetValue(statusCodeResult.Value)?.ToString();
			message.Should().Be("Server configuration error: No access token configured");
		}

		[Fact]
		public void ValidateToken_WithValidToken_ReturnsOk()
		{
			// Arrange
			const string validToken = "validtoken123";
			_controller.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Returns(validToken);

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();
			
			var message = okResult.Value!.GetType().GetProperty("message")?.GetValue(okResult.Value)?.ToString();
			var isValid = okResult.Value.GetType().GetProperty("isValid")?.GetValue(okResult.Value);
			
			message.Should().Be("Token is valid");
			isValid.Should().Be(true);
		}

		[Fact]
		public void ValidateToken_WithInvalidToken_ReturnsUnauthorized()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "Bearer invalidtoken";
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Returns("validtoken123");

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<UnauthorizedObjectResult>();
			var unauthorizedResult = (UnauthorizedObjectResult)result;
			unauthorizedResult.Value.Should().NotBeNull();
			
			var message = unauthorizedResult.Value!.GetType().GetProperty("message")?.GetValue(unauthorizedResult.Value)?.ToString();
			var isValid = unauthorizedResult.Value.GetType().GetProperty("isValid")?.GetValue(unauthorizedResult.Value);
			
			message.Should().Be("Invalid token");
			isValid.Should().Be(false);
		}

		[Fact]
		public void ValidateToken_WithValidTokenContainingWhitespace_ReturnsOk()
		{
			// Arrange
			const string validToken = "validtoken123";
			_controller.HttpContext.Request.Headers.Authorization = $"Bearer  {validToken}  ";
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Returns(validToken);

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();
			
			var isValid = okResult.Value!.GetType().GetProperty("isValid")?.GetValue(okResult.Value);
			isValid.Should().Be(true);
		}

		[Fact]
		public void ValidateToken_TokenComparisonIsCaseSensitive_ReturnsUnauthorized()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "Bearer ValidToken123";
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Returns("validtoken123");

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<UnauthorizedObjectResult>();
			var unauthorizedResult = (UnauthorizedObjectResult)result;
			unauthorizedResult.Value.Should().NotBeNull();
			
			var isValid = unauthorizedResult.Value!.GetType().GetProperty("isValid")?.GetValue(unauthorizedResult.Value);
			isValid.Should().Be(false);
		}

		[Fact]
		public void ValidateToken_WhenApplicationSettingsThrowsException_ReturnsInternalServerError()
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = "Bearer validtoken123";
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Throws(new Exception("Configuration error"));

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().NotBeNull();
			
			var message = statusCodeResult.Value!.GetType().GetProperty("message")?.GetValue(statusCodeResult.Value)?.ToString();
			var error = statusCodeResult.Value.GetType().GetProperty("error")?.GetValue(statusCodeResult.Value)?.ToString();
			
			message.Should().Be("Token validation failed");
			error.Should().Be("Configuration error");
		}

		[Fact]
		public void HealthCheck_ReturnsOkWithHealthyStatus()
		{
			// Arrange
			var beforeCall = DateTime.UtcNow;

			// Act
			var result = _controller.HealthCheck();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();
			
			var status = okResult.Value!.GetType().GetProperty("status")?.GetValue(okResult.Value)?.ToString();
			var timestamp = okResult.Value.GetType().GetProperty("timestamp")?.GetValue(okResult.Value);
			
			status.Should().Be("healthy");
			timestamp.Should().NotBeNull();
			timestamp.Should().BeOfType<DateTime>();
			
			var timestampValue = (DateTime)timestamp!;
			timestampValue.Should().BeAfter(beforeCall);
			timestampValue.Should().BeBefore(DateTime.UtcNow.AddSeconds(1)); // Allow 1 second tolerance
		}

		[Theory]
		[InlineData("Bearer token123", "token123")]
		[InlineData("Bearer   token456   ", "token456")]
		[InlineData("Bearer complex-token_with.special@chars", "complex-token_with.special@chars")]
		public void ValidateToken_WithVariousValidTokenFormats_ExtractsTokenCorrectly(string authHeader, string expectedToken)
		{
			// Arrange
			_controller.HttpContext.Request.Headers.Authorization = authHeader;
			_mockApplicationSettings.Setup(x => x.GhostfolioAccessToken).Returns(expectedToken);

			// Act
			var result = _controller.ValidateToken();

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();
			
			var isValid = okResult.Value!.GetType().GetProperty("isValid")?.GetValue(okResult.Value);
			isValid.Should().Be(true);
		}
	}
}