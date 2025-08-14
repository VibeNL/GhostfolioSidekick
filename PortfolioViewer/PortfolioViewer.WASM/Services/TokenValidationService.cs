using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public class TokenValidationService : ITokenValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenValidationService> _logger;

        public TokenValidationService(HttpClient httpClient, IConfiguration configuration, ILogger<TokenValidationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            // First, try to validate via API if available
            try
            {
                _logger.LogDebug("Attempting API token validation...");
                
                // Try health check first to see if API is reachable
                var healthRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/health");
                var healthResponse = await _httpClient.SendAsync(healthRequest);
                
                if (healthResponse.IsSuccessStatusCode)
                {
                    _logger.LogDebug("API service is reachable, proceeding with token validation");
                    
                    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/validate");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
                    var response = await _httpClient.SendAsync(request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("API token validation successful");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("API token validation failed with status: {StatusCode}", response.StatusCode);
                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning("API health check failed with status: {StatusCode}", healthResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("API validation failed: {Message}", ex.Message);
            }

            // Fallback to client-side validation
            _logger.LogDebug("Falling back to client-side token validation");
            
            var configuredToken = _configuration["GhostfolioAccessToken"];
            
            if (string.IsNullOrEmpty(configuredToken))
            {
                _logger.LogWarning("No GhostfolioAccessToken configured in client settings");
                // If no token is configured, accept any non-empty token for development
                return !string.IsNullOrWhiteSpace(token);
            }

            var isValid = string.Equals(token, configuredToken, StringComparison.Ordinal);
            _logger.LogDebug("Client-side token validation result: {IsValid}", isValid);
            return isValid;
        }
    }
}