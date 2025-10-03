using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using GhostfolioSidekick.PortfolioViewer.ApiService.Models;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProxyController : ControllerBase
	{
		private readonly HttpClient _httpClient;
		private readonly IConfigurationHelper _configurationHelper;

		public ProxyController(IHttpClientFactory httpClientFactory, IConfigurationHelper configurationHelper)
		{
			_httpClient = httpClientFactory.CreateClient();
			_configurationHelper = configurationHelper;
			// Set default headers to mimic a browser request
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
			_httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
		}

		[HttpGet("fetch")]
		public async Task<IActionResult> Fetch([FromQuery] string url, [FromQuery] bool textOnly = false, [FromQuery] bool extractMainContent = true)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return BadRequest("URL is required.");
			}

			try
			{
				// Create HTTP request with appropriate headers
				var response = await _httpClient.GetAsync(url);

				// Check if the response is successful
				if (!response.IsSuccessStatusCode)
					return StatusCode((int)response.StatusCode, $"Failed to fetch content: {response.StatusCode}");

				// Get the content as string
				var htmlContent = await response.Content.ReadAsStringAsync();

				// Load HTML content into HtmlAgilityPack document
				var htmlDoc = new HtmlDocument();
				htmlDoc.LoadHtml(htmlContent);

				// Extract metadata
				var metadata = ExtractMetadata(htmlDoc);

				// Clean HTML by removing script tags, style tags, and other unnecessary elements
				CleanHtml(htmlDoc);

				string mainContent = string.Empty;
				if (extractMainContent)
				{
					// Try to extract main content from common article containers
					mainContent = ExtractMainContent(htmlDoc);
				}

				// Create a result object with additional metadata
				var result = new FetchResponse
				{
					Url = url,
					StatusCode = (int)response.StatusCode,
					Title = metadata.Title,
					Description = metadata.Description,
					Keywords = metadata.Keywords,
					ContentType = response.Content.Headers.ContentType?.ToString(),
					Content = textOnly ? ExtractTextFromHtml(htmlDoc) : htmlDoc.DocumentNode.OuterHtml,
					MainContent = mainContent
				};

				return Ok(result);
			}
			catch (HttpRequestException ex)
			{
				return StatusCode(500, $"Failed to fetch content: {ex.Message}");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}

		[HttpGet("google-search")]
		public async Task<IActionResult> GoogleSearch([FromQuery] string query)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return BadRequest("Query is required.");
			}

			try
			{
				// Use configuration helper to get Google Search settings
				// This will check environment variables first, then appsettings.json
				var googleConfig = _configurationHelper.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch");

				if (string.IsNullOrEmpty(googleConfig.ApiKey))
					return StatusCode(500, "Google Search API key is not configured. Set GOOGLESEARCH_APIKEY environment variable or GoogleSearch:ApiKey in appsettings.json.");

				if (string.IsNullOrEmpty(googleConfig.EngineId))
					return StatusCode(500, "Google Search Engine ID is not configured. Set GOOGLESEARCH_ENGINEID environment variable or GoogleSearch:EngineId in appsettings.json.");

				var url = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(query)}&key={googleConfig.ApiKey}&cx={googleConfig.EngineId}";
				var response = await _httpClient.GetAsync(url);

				if (!response.IsSuccessStatusCode)
					return StatusCode((int)response.StatusCode, $"Failed to fetch search results: {response.StatusCode}");

				var searchResult = await response.Content.ReadFromJsonAsync<object>();
				
				// Return the search result directly as received from Google API
				return Ok(searchResult);
			}
			catch (HttpRequestException ex)
			{
				return StatusCode(500, $"Failed to fetch search results: {ex.Message}");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}

		private static void CleanHtml(HtmlDocument htmlDoc)
		{
			// Remove script tags
			RemoveNodes(htmlDoc, "//script");

			// Remove style tags
			RemoveNodes(htmlDoc, "//style");

			// Remove comment nodes
			RemoveComments(htmlDoc.DocumentNode);

			// Remove other unnecessary elements
			RemoveNodes(htmlDoc, "//nav");
			RemoveNodes(htmlDoc, "//footer");
			RemoveNodes(htmlDoc, "//header");
			RemoveNodes(htmlDoc, "//aside");
			RemoveNodes(htmlDoc, "//form");
			RemoveNodes(htmlDoc, "//iframe");
			RemoveNodes(htmlDoc, "//noscript");
		}

		private static void RemoveNodes(HtmlDocument htmlDoc, string xpath)
		{
			var nodes = htmlDoc.DocumentNode.SelectNodes(xpath);
			if (nodes != null)
			{
				foreach (var node in nodes)
				{
					node.Remove();
				}
			}
		}

		private static void RemoveComments(HtmlNode node)
		{
			if (node.NodeType == HtmlNodeType.Comment)
			{
				node.Remove();
				return;
			}

			// Create a new list for children to avoid collection modified exception
			var childNodes = node.ChildNodes.ToList();

			foreach (var child in childNodes)
			{
				RemoveComments(child);
			}
		}

		private string ExtractTextFromHtml(HtmlDocument htmlDoc)
		{
			var sb = new StringBuilder();

			// Get text from body
			var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
			if (bodyNode != null)
			{
				ExtractTextFromNode(bodyNode, sb);
			}
			else
			{
				// If no body tag, use the whole document
				ExtractTextFromNode(htmlDoc.DocumentNode, sb);
			}

			// Clean up the text by removing extra whitespace
			string text = sb.ToString();
			text = Regex.Replace(text, @"\s+", " ");
			text = Regex.Replace(text, @"\n\s*\n", "\n");

			return text.Trim();
		}

		private static void ExtractTextFromNode(HtmlNode node, StringBuilder sb)
		{
			// Skip invisible elements
			if (IsInvisibleNode(node))
				return;

			// Extract the text from the current node
			if (node.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(node.InnerText))
			{
				sb.AppendLine(node.InnerText.Trim());
			}

			// Process all child nodes
			foreach (var child in node.ChildNodes)
			{
				ExtractTextFromNode(child, sb);
			}

			// Add line breaks for block-level elements
			if (IsBlockLevelElement(node))
			{
				sb.AppendLine();
			}
		}

		private static bool IsInvisibleNode(HtmlNode node)
		{
			// Check if the node has a style attribute with display: none or visibility: hidden
			if (node.Attributes["style"] != null)
			{
				string style = node.Attributes["style"].Value.ToLower();
				if (style.Contains("display: none") || style.Contains("visibility: hidden"))
					return true;
			}

			return false;
		}

		private static bool IsBlockLevelElement(HtmlNode node)
		{
			string[] blockElements = ["p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "article", "section", "li", "br", "hr"];
			return blockElements.Contains(node.Name.ToLower());
		}

		private string ExtractMainContent(HtmlDocument htmlDoc)
		{
			// Try to find main content using common article containers
			string[] contentSelectors = [
				"//article",
				"//main",
				"//div[contains(@class, 'content')]",
				"//div[contains(@class, 'article')]",
				"//div[contains(@class, 'post')]",
				"//div[contains(@id, 'content')]",
				"//div[contains(@id, 'article')]",
				"//div[contains(@id, 'main')]"
			];

			foreach (var selector in contentSelectors)
			{
				var contentNode = htmlDoc.DocumentNode.SelectSingleNode(selector);
				if (contentNode != null)
				{
					var sb = new StringBuilder();
					ExtractTextFromNode(contentNode, sb);
					return sb.ToString().Trim();
				}
			}

			// If no main content container found, return empty
			return string.Empty;
		}

		private static (string Title, string Description, List<string> Keywords) ExtractMetadata(HtmlDocument htmlDoc)
		{
			string title = htmlDoc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? string.Empty;

			// Try to get description from meta tags
			string description = string.Empty;
			var descriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='description']");
			if (descriptionNode != null && descriptionNode.Attributes["content"] != null)
			{
				description = descriptionNode.Attributes["content"].Value.Trim();
			}

			// Try to get keywords from meta tags
			var keywords = new List<string>();
			var keywordsNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
			if (keywordsNode != null && keywordsNode.Attributes["content"] != null)
			{
				keywords = keywordsNode.Attributes["content"].Value
					.Split(',')
					.Select(k => k.Trim())
					.Where(k => !string.IsNullOrWhiteSpace(k))
					.ToList();
			}

			return (title, description, keywords);
		}
	}
}
