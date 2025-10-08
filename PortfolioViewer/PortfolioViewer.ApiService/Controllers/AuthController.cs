using GhostfolioSidekick.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController(IApplicationSettings applicationSettings) : ControllerBase
	{
		[HttpPost("validate")]
		[HttpGet("validate")]
		public IActionResult ValidateToken()
		{
			try
			{
				// Extract the token from the Authorization header
				var authHeader = Request.Headers.Authorization.FirstOrDefault();
				if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
				{
					return Unauthorized(new { message = "Invalid authorization header format" });
				}

				var token = authHeader.Substring("Bearer ".Length).Trim();

				if (string.IsNullOrEmpty(token))
				{
					return Unauthorized(new { message = "Token is required" });
				}

				// Compare the token with the configured access token
				var configuredToken = applicationSettings.GhostfolioAccessToken;

				if (string.IsNullOrEmpty(configuredToken))
				{
					return StatusCode(500, new { message = "Server configuration error: No access token configured" });
				}

				if (string.Equals(token, configuredToken, StringComparison.Ordinal))
				{
					return Ok(new { message = "Token is valid", isValid = true });
				}

				return Unauthorized(new { message = "Invalid token", isValid = false });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Token validation failed", error = ex.Message });
			}
		}

		// Health check endpoint to verify API service is running
		[HttpGet("health")]
		public IActionResult HealthCheck()
		{
			return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
		}
	}
}