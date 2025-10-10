using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.ApiService.Controllers;
using GhostfolioSidekick.PortfolioViewer.ApiService.Models;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	public class ProxyControllerTests
	{
		private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
		private readonly Mock<IConfigurationHelper> _mockConfigurationHelper;
		private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
		private readonly HttpClient _httpClient;
		private readonly ProxyController _controller;

		public ProxyControllerTests()
		{
			_mockHttpClientFactory = new Mock<IHttpClientFactory>();
			_mockConfigurationHelper = new Mock<IConfigurationHelper>();
			_mockHttpMessageHandler = new Mock<HttpMessageHandler>();

			_httpClient = new HttpClient(_mockHttpMessageHandler.Object);
			_mockHttpClientFactory.Setup(x => x.CreateClient(string.Empty)).Returns(_httpClient);

			_controller = new ProxyController(_mockHttpClientFactory.Object, _mockConfigurationHelper.Object)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};
		}

		#region Fetch Tests

		[Fact]
		public async Task Fetch_WithNullUrl_ReturnsBadRequest()
		{
			// Act
			var result = await _controller.Fetch(null!);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().Be("URL is required.");
		}

		[Fact]
		public async Task Fetch_WithEmptyUrl_ReturnsBadRequest()
		{
			// Act
			var result = await _controller.Fetch("");

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().Be("URL is required.");
		}

		[Fact]
		public async Task Fetch_WithWhitespaceUrl_ReturnsBadRequest()
		{
			// Act
			var result = await _controller.Fetch("   ");

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().Be("URL is required.");
		}

		[Fact]
		public async Task Fetch_WithValidUrl_ReturnsSuccessfulResponse()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = "<html><head><title>Test Title</title><meta name=\"description\" content=\"Test Description\" /><meta name=\"keywords\" content=\"test,example\" /></head><body><h1>Test Content</h1></body></html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().BeOfType<FetchResponse>();

			var fetchResponse = (FetchResponse)okResult.Value!;
			fetchResponse.Url.Should().Be(testUrl);
			fetchResponse.StatusCode.Should().Be(200);
			fetchResponse.Title.Should().Be("Test Title");
			fetchResponse.Description.Should().Be("Test Description");
			fetchResponse.Keywords.Should().Contain("test");
			fetchResponse.Keywords.Should().Contain("example");
			fetchResponse.Content.Should().NotBeNullOrEmpty();
			fetchResponse.ContentType.Should().Be("text/html; charset=utf-8");
		}

		[Fact]
		public async Task Fetch_WithTextOnlyTrue_ReturnsTextContent()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = "<html><body><h1>Test Header</h1><p>Test paragraph</p><script>alert('test');</script></body></html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl, textOnly: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().NotContain("<script>");
			fetchResponse.Content.Should().NotContain("alert");
			fetchResponse.Content.Should().Contain("Test Header");
			fetchResponse.Content.Should().Contain("Test paragraph");
		}

		[Fact]
		public async Task Fetch_WithExtractMainContentFalse_DoesNotExtractMainContent()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = "<html><body><article>Main content here</article></body></html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl, extractMainContent: false);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.MainContent.Should().BeEmpty();
		}

		[Fact]
		public async Task Fetch_WithArticleTag_ExtractsMainContent()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = "<html><body><nav>Navigation</nav><article>Main article content</article><footer>Footer</footer></body></html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.MainContent.Should().Contain("Main article content");
			fetchResponse.MainContent.Should().NotContain("Navigation");
			fetchResponse.MainContent.Should().NotContain("Footer");
		}

		[Fact]
		public async Task Fetch_WithHttpError_ReturnsStatusCodeResult()
		{
			// Arrange
			const string testUrl = "https://example.com";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(404);
			statusCodeResult.Value.Should().Be("Failed to fetch content: NotFound");
		}

		[Fact]
		public async Task Fetch_WithHttpRequestException_ReturnsInternalServerError()
		{
			// Arrange
			const string testUrl = "https://example.com";

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new HttpRequestException("Network error"));

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().Be("Failed to fetch content: Network error");
		}

		[Fact]
		public async Task Fetch_WithGeneralException_ReturnsInternalServerError()
		{
			// Arrange
			const string testUrl = "https://example.com";

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException("General error"));

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().Be("An error occurred: General error");
		}

		#endregion

		#region GoogleSearch Tests

		[Fact]
		public async Task GoogleSearch_WithNullQuery_ReturnsBadRequest()
		{
			// Act
			var result = await _controller.GoogleSearch(null!);

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().Be("Query is required.");
		}

		[Fact]
		public async Task GoogleSearch_WithEmptyQuery_ReturnsBadRequest()
		{
			// Act
			var result = await _controller.GoogleSearch("");

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().Be("Query is required.");
		}

		[Fact]
		public async Task GoogleSearch_WithWhitespaceQuery_ReturnsBadRequest()
		{
			// Act
			var result = await _controller.GoogleSearch("   ");

			// Assert
			result.Should().BeOfType<BadRequestObjectResult>();
			var badRequestResult = (BadRequestObjectResult)result;
			badRequestResult.Value.Should().Be("Query is required.");
		}

		[Fact]
		public async Task GoogleSearch_WithMissingApiKey_ReturnsInternalServerError()
		{
			// Arrange
			const string query = "test search";
			var googleConfig = new GoogleSearchConfiguration
			{
				ApiKey = "",
				EngineId = "test-engine-id"
			};

			_mockConfigurationHelper
				.Setup(x => x.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch"))
				.Returns(googleConfig);

			// Act
			var result = await _controller.GoogleSearch(query);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().Be("Google Search API key is not configured. Set GOOGLESEARCH_APIKEY environment variable or GoogleSearch:ApiKey in appsettings.json.");
		}

		[Fact]
		public async Task GoogleSearch_WithMissingEngineId_ReturnsInternalServerError()
		{
			// Arrange
			const string query = "test search";
			var googleConfig = new GoogleSearchConfiguration
			{
				ApiKey = "test-api-key",
				EngineId = ""
			};

			_mockConfigurationHelper
				.Setup(x => x.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch"))
				.Returns(googleConfig);

			// Act
			var result = await _controller.GoogleSearch(query);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().Be("Google Search Engine ID is not configured. Set GOOGLESEARCH_ENGINEID environment variable or GoogleSearch:EngineId in appsettings.json.");
		}

		[Fact]
		public async Task GoogleSearch_WithValidConfiguration_ReturnsSearchResults()
		{
			// Arrange
			const string query = "test search";
			var googleConfig = new GoogleSearchConfiguration
			{
				ApiKey = "test-api-key",
				EngineId = "test-engine-id"
			};

			var searchResult = new { items = new[] { new { title = "Test Result", link = "https://example.com", snippet = "Test snippet" } } };
			var searchResultJson = JsonSerializer.Serialize(searchResult);

			_mockConfigurationHelper
				.Setup(x => x.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch"))
				.Returns(googleConfig);

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(searchResultJson, Encoding.UTF8, "application/json")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.GoogleSearch(query);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			okResult.Value.Should().NotBeNull();
		}

		[Fact]
		public async Task GoogleSearch_WithGoogleApiError_ReturnsStatusCodeResult()
		{
			// Arrange
			const string query = "test search";
			var googleConfig = new GoogleSearchConfiguration
			{
				ApiKey = "test-api-key",
				EngineId = "test-engine-id"
			};

			_mockConfigurationHelper
				.Setup(x => x.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch"))
				.Returns(googleConfig);

			var responseMessage = new HttpResponseMessage(HttpStatusCode.Forbidden);

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.GoogleSearch(query);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(403);
			statusCodeResult.Value.Should().Be("Failed to fetch search results: Forbidden");
		}

		[Fact]
		public async Task GoogleSearch_WithHttpRequestException_ReturnsInternalServerError()
		{
			// Arrange
			const string query = "test search";
			var googleConfig = new GoogleSearchConfiguration
			{
				ApiKey = "test-api-key",
				EngineId = "test-engine-id"
			};

			_mockConfigurationHelper
				.Setup(x => x.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch"))
				.Returns(googleConfig);

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new HttpRequestException("Network error"));

			// Act
			var result = await _controller.GoogleSearch(query);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().Be("Failed to fetch search results: Network error");
		}

		[Fact]
		public async Task GoogleSearch_WithGeneralException_ReturnsInternalServerError()
		{
			// Arrange
			const string query = "test search";
			var googleConfig = new GoogleSearchConfiguration
			{
				ApiKey = "test-api-key",
				EngineId = "test-engine-id"
			};

			_mockConfigurationHelper
				.Setup(x => x.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch"))
				.Returns(googleConfig);

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException("General error"));

			// Act
			var result = await _controller.GoogleSearch(query);

			// Assert
			result.Should().BeOfType<ObjectResult>();
			var statusCodeResult = (ObjectResult)result;
			statusCodeResult.StatusCode.Should().Be(500);
			statusCodeResult.Value.Should().Be("An error occurred: General error");
		}

		[Fact]
		public async Task GoogleSearch_EscapesQueryParameters()
		{
			// Arrange
			const string query = "test & search with special chars @#$";
			var googleConfig = new GoogleSearchConfiguration
			{
				ApiKey = "test-api-key",
				EngineId = "test-engine-id"
			};

			_mockConfigurationHelper
				.Setup(x => x.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch"))
				.Returns(googleConfig);

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", Encoding.UTF8, "application/json")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.GoogleSearch(query);

			// Assert
			result.Should().BeOfType<OkObjectResult>();

			// Verify a HTTP request was made
			_mockHttpMessageHandler.Protected().Verify(
				"SendAsync",
				Times.Once(),
				ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>());
		}

		#endregion

		#region HTML Parsing Tests

		[Fact]
		public async Task Fetch_RemovesScriptAndStyleTags()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<head>
						<style>body { color: red; }</style>
					</head>
					<body>
						<h1>Title</h1>
						<script>alert('test');</script>
						<p>Content</p>
					</body>
				</html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().NotContain("<script>");
			fetchResponse.Content.Should().NotContain("<style>");
			fetchResponse.Content.Should().NotContain("alert('test');");
			fetchResponse.Content.Should().NotContain("color: red;");
			fetchResponse.Content.Should().Contain("<h1>Title</h1>");
			fetchResponse.Content.Should().Contain("<p>Content</p>");
		}

		[Fact]
		public async Task Fetch_RemovesNavigationAndFooterElements()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<nav>Navigation menu</nav>
						<header>Header content</header>
						<main>Main content</main>
						<aside>Sidebar content</aside>
						<footer>Footer content</footer>
					</body>
				</html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().NotContain("<nav>");
			fetchResponse.Content.Should().NotContain("<header>");
			fetchResponse.Content.Should().NotContain("<aside>");
			fetchResponse.Content.Should().NotContain("<footer>");
			fetchResponse.Content.Should().NotContain("Navigation menu");
			fetchResponse.Content.Should().NotContain("Header content");
			fetchResponse.Content.Should().NotContain("Sidebar content");
			fetchResponse.Content.Should().NotContain("Footer content");
			fetchResponse.Content.Should().Contain("Main content");
		}

		[Fact]
		public async Task Fetch_ExtractsMetadataCorrectly()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<head>
						<title>Test Page Title</title>
						<meta name=""description"" content=""This is a test page description"" />
						<meta name=""keywords"" content=""test, page, example, demo"" />
					</head>
					<body>
						<h1>Content</h1>
					</body>
				</html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.Title.Should().Be("Test Page Title");
			fetchResponse.Description.Should().Be("This is a test page description");
			fetchResponse.Keywords.Should().HaveCount(4);
			fetchResponse.Keywords.Should().Contain("test");
			fetchResponse.Keywords.Should().Contain("page");
			fetchResponse.Keywords.Should().Contain("example");
			fetchResponse.Keywords.Should().Contain("demo");
		}

		[Fact]
		public async Task Fetch_HandlesHtmlWithoutMetadata()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<h1>Content without metadata</h1>
					</body>
				</html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.Title.Should().BeEmpty();
			fetchResponse.Description.Should().BeEmpty();
			fetchResponse.Keywords.Should().BeEmpty();
		}

		[Fact]
		public async Task Fetch_ExtractsMainContentFromDifferentSelectors()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div class=""content"">
							<h1>Main Content Title</h1>
							<p>This is the main content.</p>
						</div>
						<div class=""sidebar"">Sidebar content</div>
					</body>
				</html>";

			var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(testHtml, Encoding.UTF8, "text/html")
			};

			_mockHttpMessageHandler.Protected()
				.Setup<Task<HttpResponseMessage>>(
					"SendAsync",
					ItExpr.IsAny<HttpRequestMessage>(),
					ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(responseMessage);

			// Act
			var result = await _controller.Fetch(testUrl);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (FetchResponse)okResult.Value!;

			fetchResponse.MainContent.Should().Contain("Main Content Title");
			fetchResponse.MainContent.Should().Contain("This is the main content.");
			fetchResponse.MainContent.Should().NotContain("Sidebar content");
		}

		#endregion

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_httpClient?.Dispose();
			}
		}
	}
}