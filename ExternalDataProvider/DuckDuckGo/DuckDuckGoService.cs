using System.Net.Http.Json;

namespace GhostfolioSidekick.ExternalDataProvider.DuckDuckGo
{
    public class DuckDuckGoService(HttpClient httpClient)
	{
		public async Task<DuckDuckGoResult?> SearchAsync(string query)
        {
            var url = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json";
            return await httpClient.GetFromJsonAsync<DuckDuckGoResult>(url);
        }
    }

    // Simplified model for DuckDuckGo Instant Answer API
    public class DuckDuckGoResult
    {
        public string? Heading { get; set; }
        public string? Abstract { get; set; }
        public string? Answer { get; set; }
        public string? Definition { get; set; }
        public string? Redirect { get; set; }
        // Add more properties as needed from the API response
    }
}
