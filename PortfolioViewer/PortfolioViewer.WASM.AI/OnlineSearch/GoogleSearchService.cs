using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch
{
	/// <summary>
	/// Service for performing Google searches and retrieving web content
	/// Implements the Model-Context-Protocol pattern
	/// </summary>
	public class GoogleSearchService : IGoogleSearchProtocol
	{
		private readonly GoogleSearchContext _context;

		/// <summary>
		/// Initializes a new instance of the GoogleSearchService
		/// </summary>
		/// <param name="httpClient">HttpClient for making API calls</param>
		/// <param name="apiKey">Optional Google API Key (typically provided by backend)</param>
		/// <param name="cx">Optional Google Custom Search Engine ID (typically provided by backend)</param>
		public GoogleSearchService(HttpClient httpClient, string? apiKey = null, string? cx = null)
		{
			_context = new GoogleSearchContext
			{
				HttpClient = httpClient,
				ApiKey = apiKey,
				CustomSearchEngineId = cx
			};
		}

		/// <summary>
		/// Initializes a new instance of the GoogleSearchService with a specific context
		/// </summary>
		/// <param name="context">The Google Search context</param>
		public GoogleSearchService(GoogleSearchContext context)
		{
			_context = context;
		}

		/// <summary>
		/// Performs a search using the provided query
		/// </summary>
		/// <param name="request">The search request containing the query</param>
		/// <returns>A response with search results</returns>
		public async Task<GoogleSearchResponse> SearchAsync(GoogleSearchRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Query))
			{
				return new GoogleSearchResponse
				{
					Success = false,
					ErrorMessage = "Search query cannot be empty"
				};
			}

			try
			{
				var url = $"{_context.BackendGoogleSearchUrl}{Uri.EscapeDataString(request.Query.Trim())}";
				var response = await _context.HttpClient.GetAsync(url);

				if (!response.IsSuccessStatusCode)
				{
					return new GoogleSearchResponse
					{
						Success = false,
						ErrorMessage = $"Search request failed with status code: {response.StatusCode}"
					};
				}

				var result = await response.Content.ReadFromJsonAsync<GoogleSearchApiResult>();
				if (result == null || result.Items == null || result.Items.Count == 0)
				{
					return new GoogleSearchResponse
					{
						Success = true,
						Results = Array.Empty<WebResult>()
					};
				}

				var webResults = new List<WebResult>();
				foreach (var item in result.Items)
				{
					string? content = await GetWebsiteContentAsync(item.Link);

					webResults.Add(new WebResult
					{
						Title = item.Title,
						Link = item.Link,
						Snippet = item.Snippet,
						Content = content
					});
				}

				return new GoogleSearchResponse
				{
					Success = true,
					Results = webResults
				};
			}
			catch (Exception ex)
			{
				return new GoogleSearchResponse
				{
					Success = false,
					ErrorMessage = $"Search failed: {ex.Message}"
				};
			}
		}

		/// <summary>
		/// For compatibility with existing code - to be deprecated
		/// </summary>
		/// <param name="query">The search query</param>
		/// <returns>Collection of WebResult objects</returns>
		public async Task<ICollection<WebResult>> SearchAsync(string query)
		{
			var request = new GoogleSearchRequest { Query = query };
			var response = await SearchAsync(request);
			return response.Success ? response.Results.ToList() : [];
		}

		/// <summary>
		/// Retrieves the content of a website
		/// </summary>
		/// <param name="url">The URL of the website to retrieve content from</param>
		/// <returns>The website content as a string, or null if unsuccessful</returns>
		public async Task<string?> GetWebsiteContentAsync(string? url)
		{
			if (string.IsNullOrEmpty(url))
				return null;
			try
			{
				// Call backend proxy instead of direct fetch
				var encodedUrl = Uri.EscapeDataString(url);
				var response = await _context.HttpClient.GetAsync(_context.BackendProxyUrl + encodedUrl);
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadAsStringAsync();
				}
			}
			catch (Exception)
			{
				// Ignore errors and leave content as null
			}

			return null;
		}
	}
}
