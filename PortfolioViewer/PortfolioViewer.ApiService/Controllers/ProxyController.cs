using GhostfolioSidekick.PortfolioViewer.ApiService.Models;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public partial class ProxyController : ControllerBase
	{
		private const string contentString = "content";
		private readonly HttpClient _httpClient;
		private readonly IConfigurationHelper _configurationHelper;

		// Allowed URL schemes for security
		private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase) { "http", "https" };

		// Blocked private/internal IP ranges and localhost
		private static readonly List<IPNetwork> BlockedNetworks =
		[
			IPNetwork.Parse("127.0.0.0/8"),     // Loopback
			IPNetwork.Parse("10.0.0.0/8"),      // Private Class A
			IPNetwork.Parse("172.16.0.0/12"),   // Private Class B
			IPNetwork.Parse("192.168.0.0/16"),  // Private Class C
			IPNetwork.Parse("169.254.0.0/16"),  // Link-local
			IPNetwork.Parse("224.0.0.0/4"),     // Multicast
			IPNetwork.Parse("::1/128"),         // IPv6 loopback
			IPNetwork.Parse("fc00::/7"),        // IPv6 unique local
			IPNetwork.Parse("fe80::/10")        // IPv6 link-local
		];

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

			// Validate and sanitize the URL
			var validationResult = await ValidateUrlAsync(url);
			if (!validationResult.IsValid)
			{
				return BadRequest($"Invalid URL: {validationResult.ErrorMessage}");
			}

			try
			{
				// Use the validated URL - validationResult.ValidatedUrl is guaranteed to be non-null when IsValid is true
				var response = await _httpClient.GetAsync(validationResult.ValidatedUrl!);

				// Check if the response is successful
				if (!response.IsSuccessStatusCode)
					return StatusCode((int)response.StatusCode, $"Failed to fetch content: {response.StatusCode}");

				// Get the content as string
				var htmlContent = await response.Content.ReadAsStringAsync();

				// Load HTML content into HtmlAgilityPack document
				var htmlDoc = new HtmlDocument();
				htmlDoc.LoadHtml(htmlContent);

				// Extract metadata
				var (Title, Description, Keywords) = ExtractMetadata(htmlDoc);

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
					Url = validationResult.ValidatedUrl!.ToString(),
					StatusCode = (int)response.StatusCode,
					Title = Title,
					Description = Description,
					Keywords = Keywords,
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

		private static async Task<UrlValidationResult> ValidateUrlAsync(string url)
		{
			// Try to parse the URL
			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsedUri))
			{
				return new UrlValidationResult { IsValid = false, ErrorMessage = "Invalid URL format" };
			}

			// Check if scheme is allowed
			if (!AllowedSchemes.Contains(parsedUri.Scheme))
			{
				return new UrlValidationResult { IsValid = false, ErrorMessage = $"URL scheme '{parsedUri.Scheme}' is not allowed" };
			}

			// Resolve the hostname to IP addresses
			try
			{
				var hostEntry = await Dns.GetHostEntryAsync(parsedUri.Host);

				// Check each resolved IP address using LINQ
				var blockedIp = hostEntry.AddressList
					.FirstOrDefault(ipAddress => BlockedNetworks.Any(blockedNetwork => blockedNetwork.Contains(ipAddress)));

				if (blockedIp != null)
				{
					return new UrlValidationResult { IsValid = false, ErrorMessage = "Access to private/internal networks is not allowed" };
				}
			}
			catch (Exception)
			{
				// If DNS resolution fails, block the request
				return new UrlValidationResult { IsValid = false, ErrorMessage = "Unable to resolve hostname" };
			}

			// Additional port validation - block common internal service ports
			var blockedPorts = new HashSet<int> { 22, 23, 25, 53, 110, 143, 993, 995, 1433, 3306, 5432, 6379, 11211, 27017 };
			if (blockedPorts.Contains(parsedUri.Port))
			{
				return new UrlValidationResult { IsValid = false, ErrorMessage = $"Access to port {parsedUri.Port} is not allowed" };
			}

			return new UrlValidationResult { IsValid = true, ValidatedUrl = parsedUri };
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

		private static string ExtractTextFromHtml(HtmlDocument htmlDoc)
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
			text = WhitespaceRegex().Replace(text, " ");
			text = NewlineRegex().Replace(text, "\n");

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

		private static string ExtractMainContent(HtmlDocument htmlDoc)
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
			if (descriptionNode != null && descriptionNode.Attributes[contentString] != null)
			{
				description = descriptionNode.Attributes[contentString].Value.Trim();
			}

			// Try to get keywords from meta tags
			var keywords = new List<string>();
			var keywordsNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
			if (keywordsNode != null && keywordsNode.Attributes[contentString] != null)
			{
				keywords = [.. keywordsNode.Attributes[contentString].Value
					.Split(',')
					.Select(k => k.Trim())
					.Where(k => !string.IsNullOrWhiteSpace(k))];
			}

			return (title, description, keywords);
		}

		[GeneratedRegex(@"\s+")]
		private static partial Regex WhitespaceRegex();

		[GeneratedRegex(@"\n\s*\n")]
		private static partial Regex NewlineRegex();
	}

	internal class UrlValidationResult
	{
		public bool IsValid { get; set; }
		public string ErrorMessage { get; set; } = string.Empty;
		public Uri? ValidatedUrl { get; set; }
	}

	// Helper class for IP network validation
	internal class IPNetwork
	{
		private readonly IPAddress _network;
		private readonly byte[] _maskBytes;

		private IPNetwork(IPAddress network, int prefixLength)
		{
			_network = network;
			_maskBytes = CreateMaskBytes(network.AddressFamily, prefixLength);
		}

		public static IPNetwork Parse(string cidr)
		{
			var parts = cidr.Split('/');
			if (parts.Length != 2)
				throw new ArgumentException("Invalid CIDR format");

			var network = IPAddress.Parse(parts[0]);
			var prefixLength = int.Parse(parts[1]);

			return new IPNetwork(network, prefixLength);
		}

		public bool Contains(IPAddress address)
		{
			if (address.AddressFamily != _network.AddressFamily)
				return false;

			var addressBytes = address.GetAddressBytes();
			var networkBytes = _network.GetAddressBytes();

			// Apply mask to both addresses and compare
			for (int i = 0; i < addressBytes.Length; i++)
			{
				if ((addressBytes[i] & _maskBytes[i]) != (networkBytes[i] & _maskBytes[i]))
					return false;
			}

			return true;
		}

		private static byte[] CreateMaskBytes(System.Net.Sockets.AddressFamily addressFamily, int prefixLength)
		{
			int totalBits = addressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
			int bytes = totalBits / 8;
			var mask = new byte[bytes];

			int fullBytes = prefixLength / 8;
			int remainingBits = prefixLength % 8;

			// Set full bytes to 255
			for (int i = 0; i < fullBytes; i++)
			{
				mask[i] = 255;
			}

			// Set partial byte
			if (remainingBits > 0 && fullBytes < bytes)
			{
				mask[fullBytes] = (byte)(255 << (8 - remainingBits));
			}

			return mask;
		}
	}
}
