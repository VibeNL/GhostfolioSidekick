namespace GhostfolioSidekick.PortfolioViewer.ApiService.Models
{
    /// <summary>
    /// Models for the Proxy controller
    /// </summary>
    
    /// <summary>
    /// Response model for the fetch endpoint
    /// </summary>
    public class FetchResponse
    {
        /// <summary>
        /// The original URL that was requested
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// The HTTP status code of the response
        /// </summary>
        public int StatusCode { get; set; }
        
        /// <summary>
        /// The title of the webpage
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// The description from meta tags
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Keywords from meta tags
        /// </summary>
        public List<string> Keywords { get; set; } = [];
        
        /// <summary>
        /// The content type of the response
        /// </summary>
        public string? ContentType { get; set; }
        
        /// <summary>
        /// The content of the webpage
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// The main content of the webpage, if extracted
        /// </summary>
        public string MainContent { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Google Search Result Model for API responses
    /// </summary>
    public class GoogleSearchApiResponse
    {
        /// <summary>
        /// Items returned by the search
        /// </summary>
        public List<GoogleSearchItem>? Items { get; set; }
    }

    /// <summary>
    /// Individual search result item
    /// </summary>
    public class GoogleSearchItem
    {
        /// <summary>
        /// Title of the search result
        /// </summary>
        public string? Title { get; set; }
        
        /// <summary>
        /// Link to the search result
        /// </summary>
        public string? Link { get; set; }
        
        /// <summary>
        /// Snippet of text from the search result
        /// </summary>
        public string? Snippet { get; set; }
    }
}