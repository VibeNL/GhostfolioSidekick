using System.Net.Http;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch
{
    /// <summary>
    /// Context for Google Search operations
    /// </summary>
    public record GoogleSearchContext
    {
        /// <summary>
        /// HttpClient for making API calls
        /// </summary>
        public required HttpClient HttpClient { get; init; }
        
        /// <summary>
        /// URL for the backend proxy used to fetch web content
        /// </summary>
        public string BackendProxyUrl { get; init; } = "/api/proxy/fetch?url=";
        
        /// <summary>
        /// URL for the backend Google Search API
        /// </summary>
        public string BackendGoogleSearchUrl { get; init; } = "/api/proxy/google-search?query=";
        
        /// <summary>
        /// Optional Google API Key (typically provided by backend)
        /// </summary>
        public string? ApiKey { get; init; }
        
        /// <summary>
        /// Optional Google Custom Search Engine ID (typically provided by backend)
        /// </summary>
        public string? CustomSearchEngineId { get; init; }
    }
}