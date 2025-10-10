namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class TokenValidationService(HttpClient httpClient, ILogger<TokenValidationService> logger) : ITokenValidationService
	{
		public async Task<bool> ValidateTokenAsync(string token)
		{
			// First, try to validate via API if available
			try
			{
				logger.LogDebug("Attempting API token validation...");

				// Try health check first to see if API is reachable
				var healthRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/health");
				var healthResponse = await httpClient.SendAsync(healthRequest);

				if (healthResponse.IsSuccessStatusCode)
				{
					logger.LogDebug("API service is reachable, proceeding with token validation");

					var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/validate");
					request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

					var response = await httpClient.SendAsync(request);

					if (response.IsSuccessStatusCode)
					{
						logger.LogDebug("API token validation successful");
						return true;
					}
					else
					{
						logger.LogWarning("API token validation failed with status: {StatusCode}", response.StatusCode);
						return false;
					}
				}
				else
				{
					logger.LogWarning("API health check failed with status: {StatusCode}", healthResponse.StatusCode);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "API validation failed");
			}

			return false;
		}
	}
}