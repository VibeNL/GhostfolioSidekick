namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch.Models
{
    // Models for Google Search Service
    
    /// <summary>
    /// Represents a Google search request
    /// </summary>
    public record GoogleSearchRequest
    {
        /// <summary>
        /// The search query text
        /// </summary>
        public required string Query { get; init; }
    }

    /// <summary>
    /// Represents a Google search response
    /// </summary>
    public record GoogleSearchResponse
    {
        /// <summary>
        /// Collection of search results
        /// </summary>
        public IReadOnlyCollection<WebResult> Results { get; init; } = Array.Empty<WebResult>();
        
        /// <summary>
        /// Indicates if the search was successful
        /// </summary>
        public bool Success { get; init; }
        
        /// <summary>
        /// Error message if the search failed
        /// </summary>
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Represents the raw Google Search API result
    /// </summary>
    public class GoogleSearchApiResult
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