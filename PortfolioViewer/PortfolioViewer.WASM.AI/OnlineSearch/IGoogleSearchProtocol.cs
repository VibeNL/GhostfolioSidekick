namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch
{
    /// <summary>
    /// Protocol (interface) for Google Search operations
    /// </summary>
    public interface IGoogleSearchProtocol
    {
        /// <summary>
        /// Performs a search using the provided query
        /// </summary>
        /// <param name="request">The search request containing the query</param>
        /// <returns>A response with search results</returns>
        Task<GoogleSearchResponse> SearchAsync(GoogleSearchRequest request);
        
        /// <summary>
        /// For compatibility with existing code
        /// </summary>
        /// <param name="query">Search query</param>
        /// <returns>Collection of search results</returns>
        Task<ICollection<WebResult>> SearchAsync(string query);
        
        /// <summary>
        /// Retrieves the content of a website
        /// </summary>
        /// <param name="url">The URL of the website to retrieve content from</param>
        /// <returns>The website content as a string, or null if unsuccessful</returns>
        Task<string?> GetWebsiteContentAsync(string? url);
    }
}