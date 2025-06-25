using System.Net.Http.Json;

namespace GhostfolioSidekick.ExternalDataProvider.Google
{
    public class GoogleSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _backendProxyUrl = "/api/proxy/fetch?url="; // Adjust base URL as needed
        private readonly string apiKey;
        private readonly string cx;

        public GoogleSearchService(HttpClient httpClient, string apiKey, string cx)
        {
            this._httpClient = httpClient;
            this.apiKey = apiKey;
            this.cx = cx;
        }

        public async Task<ICollection<WebResult>> SearchAsync(string query)
        {
            var url = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(query)}&key={apiKey}&cx={cx}";
            var results = await _httpClient.GetFromJsonAsync<GoogleSearchResult>(url);
            if (results == null || results.Items == null || results.Items.Count == 0)
            {
                return [];
            }

            var webResults = new List<WebResult>();
            foreach (var item in results.Items)
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
