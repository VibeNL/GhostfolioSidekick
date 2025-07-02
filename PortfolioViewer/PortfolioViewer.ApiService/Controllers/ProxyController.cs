using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using GhostfolioSidekick.PortfolioViewer.ApiService.Models;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProxyController : ControllerBase
	{
		private readonly HttpClient _httpClient;
		private readonly IConfiguration _configuration;

		public ProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClient = httpClientFactory.CreateClient();
			_configuration = configuration;
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
				var apiKey = _configuration["GoogleSearch:ApiKey"];
				var cx = _configuration["GoogleSearch:EngineId"];

				if (string.IsNullOrEmpty(apiKey))
					return StatusCode(500, "Google Search API key is not configured.");

				if (string.IsNullOrEmpty(cx))
					return StatusCode(500, "Google Search Engine ID is not configured.");

				var url = $"https://www.googleapis.com/customsearch/v1?q={Uri.EscapeDataString(query)}&key={apiKey}&cx={cx}";
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

		[HttpGet("download-model")]
		public async Task<IActionResult> DownloadModel([FromQuery] string url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return BadRequest("URL is required.");
			}

			try
			{
				// Validate that the URL is from a trusted source (Hugging Face)
				var uri = new Uri(url);
				if (!uri.Host.Contains("huggingface.co"))
				{
					return BadRequest("Only Hugging Face model downloads are allowed.");
				 }

				// Create a dedicated HttpClient for large downloads with extended timeout
				using var downloadClient = new HttpClient();
				downloadClient.Timeout = TimeSpan.FromHours(2); // 2 hours for very large downloads
				downloadClient.DefaultRequestHeaders.Add("User-Agent", "PortfolioViewer-Proxy/1.0");
				downloadClient.DefaultRequestHeaders.Add("Accept", "*/*");
				
				// First, try to get headers to validate the download
				using var headResponse = await downloadClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
				if (!headResponse.IsSuccessStatusCode)
				{
					return StatusCode((int)headResponse.StatusCode, $"Model not accessible: {headResponse.StatusCode}");
				}

				var contentLength = headResponse.Content.Headers.ContentLength;
				var contentType = headResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
				var supportsRanges = headResponse.Headers.AcceptRanges?.Contains("bytes") == true;

				// Log the download attempt
				Console.WriteLine($"Starting model download: {url}");
				Console.WriteLine($"Size: {contentLength?.ToString() ?? "unknown"} bytes");
				Console.WriteLine($"Content-Type: {contentType}");
				Console.WriteLine($"Supports Range Requests: {supportsRanges}");

				// Check if client requested a range
				var rangeHeader = Request.Headers["Range"].FirstOrDefault();
				if (!string.IsNullOrEmpty(rangeHeader) && supportsRanges)
				{
					Console.WriteLine($"Client requested range: {rangeHeader}");
					// Forward the range request
					var request = new HttpRequestMessage(HttpMethod.Get, url);
					request.Headers.Add("Range", rangeHeader);
					
					using var rangeResponse = await downloadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
					
					if (rangeResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
					{
						// Return partial content
						Response.StatusCode = 206;
						var data = await rangeResponse.Content.ReadAsByteArrayAsync();
						
						// Forward range-related headers
						if (rangeResponse.Content.Headers.TryGetValues("Content-Range", out var contentRangeValues))
						{
							Response.Headers.Append("Content-Range", contentRangeValues.First());
						}
						Response.Headers.Append("Accept-Ranges", "bytes");
						Response.Headers.Append("Content-Length", data.Length.ToString());
						
						return File(data, contentType);
					}
				}

				// Stream the full model file directly to the client
				using var response = await downloadClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
				
				if (!response.IsSuccessStatusCode)
				{
					return StatusCode((int)response.StatusCode, $"Failed to download model: {response.StatusCode}");
				}

				// Set appropriate headers for the download
				if (contentLength.HasValue)
				{
					Response.Headers.Append("Content-Length", contentLength.Value.ToString());
				}
				Response.Headers.Append("Content-Type", contentType);
				Response.Headers.Append("Cache-Control", "public, max-age=31536000"); // Cache for 1 year
				
				if (supportsRanges)
				{
					Response.Headers.Append("Accept-Ranges", "bytes"); // Support range requests
				}
				
				// Return the stream directly to avoid loading the entire 2.4GB file into memory
				var stream = await response.Content.ReadAsStreamAsync();
				return new FileStreamResult(stream, contentType)
				{
					EnableRangeProcessing = supportsRanges
				};
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
			{
				return StatusCode(408, "Request timeout - the model download took too long.");
			}
			catch (HttpRequestException ex)
			{
				return StatusCode(500, $"Failed to download model: {ex.Message}");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred during model download: {ex.Message}");
			}
		}

		[HttpGet("download-model-range")]
		public async Task<IActionResult> DownloadModelRange(
			[FromQuery] string url, 
			[FromQuery] long start, 
			[FromQuery] long end)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return BadRequest("URL is required.");
			}

			if (start < 0 || end < start)
			{
				return BadRequest("Invalid range: start must be >= 0 and end must be >= start.");
			}

			try
			{
				// Validate that the URL is from a trusted source (Hugging Face)
				var uri = new Uri(url);
				if (!uri.Host.Contains("huggingface.co"))
				{
					return BadRequest("Only Hugging Face model downloads are allowed.");
				}

				// Create a dedicated HttpClient for range requests
				using var rangeClient = new HttpClient();
				rangeClient.Timeout = TimeSpan.FromMinutes(10); // 10 minutes for chunk downloads
				rangeClient.DefaultRequestHeaders.Add("User-Agent", "PortfolioViewer-Proxy/1.0");
				rangeClient.DefaultRequestHeaders.Add("Accept", "*/*");
				
				// Create range request
				var request = new HttpRequestMessage(HttpMethod.Get, url);
				request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

				Console.WriteLine($"Range request: {url}, bytes {start}-{end} ({end - start + 1} bytes)");

				using var response = await rangeClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
				
				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine($"Range request failed: {response.StatusCode} - {response.ReasonPhrase}");
					return StatusCode((int)response.StatusCode, $"Failed to download range: {response.StatusCode} - {response.ReasonPhrase}");
				}

				// Handle both 206 (Partial Content) and 200 (OK) responses
				// Some servers might return 200 OK even for range requests
				if (response.StatusCode != System.Net.HttpStatusCode.PartialContent && 
					response.StatusCode != System.Net.HttpStatusCode.OK)
				{
					return StatusCode((int)response.StatusCode, $"Unexpected response for range request: {response.StatusCode}");
				}

				var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
				var contentLength = response.Content.Headers.ContentLength;
				
				// Get the chunk data
				var chunkData = await response.Content.ReadAsByteArrayAsync();
				
				Console.WriteLine($"Range response: {chunkData.Length} bytes delivered (Content-Length: {contentLength})");

				// Verify the chunk size matches what we expected
				var expectedSize = end - start + 1;
				if (chunkData.Length != expectedSize && response.StatusCode == System.Net.HttpStatusCode.PartialContent)
				{
					Console.WriteLine($"Warning: Chunk size mismatch. Expected: {expectedSize}, Got: {chunkData.Length}");
				}

				// Create the result with proper headers
				var result = new FileContentResult(chunkData, contentType);
				
				// Set appropriate response headers based on the response type
				if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
				{
					Response.StatusCode = 206; // Partial Content
					Response.Headers.Append("Accept-Ranges", "bytes");
					
					 // Try to get Content-Range from the upstream response
					var contentRangeHeader = response.Content.Headers.FirstOrDefault(h => 
						h.Key.Equals("Content-Range", StringComparison.OrdinalIgnoreCase));
					
					if (!contentRangeHeader.Equals(default(KeyValuePair<string, IEnumerable<string>>)))
					{
						var contentRange = contentRangeHeader.Value.FirstOrDefault();
						if (!string.IsNullOrEmpty(contentRange))
						{
							Response.Headers.Append("Content-Range", contentRange);
							Console.WriteLine($"Forwarding Content-Range: {contentRange}");
						}
					}
					else
					{
						// Construct Content-Range header manually for partial content
						// Format: bytes start-end/total (we don't know total, so use *)
						var constructedRange = $"bytes {start}-{start + chunkData.Length - 1}/*";
						Response.Headers.Append("Content-Range", constructedRange);
						Console.WriteLine($"Constructed Content-Range: {constructedRange}");
					}
				}
				else
				{
					// For 200 OK responses, still indicate range support for future requests
					Response.Headers.Append("Accept-Ranges", "bytes");
					Console.WriteLine("Server returned 200 OK for range request (full content or no range support)");
				}

				// Set content length if available
				Response.Headers.Append("Content-Length", chunkData.Length.ToString());

				return result;
			}
			catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
			{
				Console.WriteLine($"Range request timeout: {ex.Message}");
				return StatusCode(408, "Range request timeout.");
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"Range request HTTP error: {ex.Message}");
				return StatusCode(500, $"Failed to download range: {ex.Message}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Range request unexpected error: {ex.Message}");
				return StatusCode(500, $"An error occurred during range download: {ex.Message}");
			}
		}

		private void CleanHtml(HtmlDocument htmlDoc)
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

		private void RemoveNodes(HtmlDocument htmlDoc, string xpath)
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

		private void RemoveComments(HtmlNode node)
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

		private void ExtractTextFromNode(HtmlNode node, StringBuilder sb)
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

		private bool IsInvisibleNode(HtmlNode node)
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

		private bool IsBlockLevelElement(HtmlNode node)
		{
			string[] blockElements = { "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "article", "section", "li", "br", "hr" };
			return blockElements.Contains(node.Name.ToLower());
		}

		private string ExtractMainContent(HtmlDocument htmlDoc)
		{
			// Try to find main content using common article containers
			string[] contentSelectors = {
				"//article",
				"//main",
				"//div[contains(@class, 'content')]",
				"//div[contains(@class, 'article')]",
				"//div[contains(@class, 'post')]",
				"//div[contains(@id, 'content')]",
				"//div[contains(@id, 'article')]",
				"//div[contains(@id, 'main')]"
			};

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

		private (string Title, string Description, List<string> Keywords) ExtractMetadata(HtmlDocument htmlDoc)
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
