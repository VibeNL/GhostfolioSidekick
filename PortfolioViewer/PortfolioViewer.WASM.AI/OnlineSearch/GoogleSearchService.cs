using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch
{
    public class GoogleSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _backendProxyUrl = "/api/proxy/fetch?url="; // Proxy URL for content fetch
        private readonly string _backendGoogleSearchUrl = "/api/proxy/google-search?query="; // New endpoint for Google Search API

        public GoogleSearchService(HttpClient httpClient, string apiKey = null, string cx = null)
        {
			_httpClient = httpClient;
            // API key and CX are now ignored as they will be provided by the backend
        }

        public async Task<ICollection<WebResult>> SearchAsync(string query)
        {
            var url = $"{_backendGoogleSearchUrl}{Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }
            
            var result = await response.Content.ReadFromJsonAsync<GoogleSearchResult>();
            if (result == null || result.Items == null || result.Items.Count == 0)
            {
                return [];
            }

            var webResults = new List<WebResult>();
            foreach (var item in result.Items)
            {
                string? content = await GetContentWebsite(item);

                webResults.Add(new WebResult
                {
                    Title = item.Title,
                    Link = item.Link,
                    Snippet = item.Snippet,
                    Content = content
                });
            }

            return webResults;
        }

        private async Task<string?> GetContentWebsite(GoogleSearchResult.Item item)
        {
            if (string.IsNullOrEmpty(item.Link))
                return null;
            try
            {
                // Call backend proxy instead of direct fetch
                var encodedUrl = Uri.EscapeDataString(item.Link);
                var response = await _httpClient.GetAsync(_backendProxyUrl + encodedUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch(Exception ex)
            {
                // Ignore errors and leave content as null
            }

            return null;
        }
    }

    // Simplified model for Google Custom Search JSON API
    public class GoogleSearchResult
    {
        public List<Item>? Items { get; set; }

        public class Item
        {
            public string? Title { get; set; }
            public string? Link { get; set; }
            public string? Snippet { get; set; }
        }
    }
}
