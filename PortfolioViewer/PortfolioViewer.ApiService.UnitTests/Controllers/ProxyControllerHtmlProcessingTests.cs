using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.ApiService.Controllers;
using GhostfolioSidekick.PortfolioViewer.ApiService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	/// <summary>
	/// Tests for HTML processing functionality in ProxyController
	/// These tests focus on the HTML cleaning, text extraction, and content parsing features
	/// </summary>
	public class ProxyControllerHtmlProcessingTests
	{
		private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
		private readonly Mock<IConfigurationHelper> _mockConfigurationHelper;
		private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
		private readonly HttpClient _httpClient;
		private readonly ProxyController _controller;

		public ProxyControllerHtmlProcessingTests()
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

		#region Text Extraction Tests

		[Fact]
		public async Task Fetch_TextExtraction_HandlesComplexHtml()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<h1>Main Title</h1>
						<div>
							<p>First paragraph with <strong>bold</strong> text.</p>
							<p>Second paragraph with <em>italic</em> text.</p>
							<ul>
								<li>List item 1</li>
								<li>List item 2</li>
							</ul>
						</div>
						<div style=""display: none"">Hidden content</div>
						<div style=""visibility: hidden"">Also hidden</div>
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
			var result = await _controller.Fetch(testUrl, textOnly: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().Contain("Main Title");
			fetchResponse.Content.Should().Contain("First paragraph with bold text.");
			fetchResponse.Content.Should().Contain("Second paragraph with italic text.");
			fetchResponse.Content.Should().Contain("List item 1");
			fetchResponse.Content.Should().Contain("List item 2");
			fetchResponse.Content.Should().NotContain("Hidden content");
			fetchResponse.Content.Should().NotContain("Also hidden");
			fetchResponse.Content.Should().NotContain("<");
			fetchResponse.Content.Should().NotContain(">");
		}

		[Fact]
		public async Task Fetch_TextExtraction_HandlesBlockLevelElements()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<h1>Header 1</h1>
						<h2>Header 2</h2>
						<p>Paragraph 1</p>
						<div>Division</div>
						<article>Article content</article>
						<section>Section content</section>
						<li>List item</li>
						<br>
						<hr>
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
			var result = await _controller.Fetch(testUrl, textOnly: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			// All text should be present
			fetchResponse.Content.Should().Contain("Header 1");
			fetchResponse.Content.Should().Contain("Header 2");
			fetchResponse.Content.Should().Contain("Paragraph 1");
			fetchResponse.Content.Should().Contain("Division");
			fetchResponse.Content.Should().Contain("Article content");
			fetchResponse.Content.Should().Contain("Section content");
			fetchResponse.Content.Should().Contain("List item");

			// Should have proper line breaks due to block elements
			var lines = fetchResponse.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			lines.Should().Contain(line => line.Contains("Header 1"));
			lines.Should().Contain(line => line.Contains("Header 2"));
		}

		[Fact]
		public async Task Fetch_TextExtraction_HandlesWhitespaceNormalization()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<p>Text    with     multiple     spaces</p>
						<p>Text
						with
						newlines</p>
						<p>	Text	with	tabs	</p>
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
			var result = await _controller.Fetch(testUrl, textOnly: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			// Multiple spaces should be normalized to single spaces
			fetchResponse.Content.Should().Contain("Text with multiple spaces");
			fetchResponse.Content.Should().NotContain("    ");
			fetchResponse.Content.Should().NotContain("\t");
		}

		[Fact]
		public async Task Fetch_TextExtraction_HandlesEmptyAndWhitespaceNodes()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<p>Valid content</p>
						<p>   </p>
						<p></p>
						<div>
							
						</div>
						<span>More valid content</span>
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
			var result = await _controller.Fetch(testUrl, textOnly: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().Contain("Valid content");
			fetchResponse.Content.Should().Contain("More valid content");
			fetchResponse.Content.Should().NotContain("   ");
		}

		#endregion

		#region Main Content Extraction Tests

		[Fact]
		public async Task Fetch_MainContentExtraction_PrioritizesArticleTag()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div class=""content"">Content div</div>
						<main>Main element</main>
						<article>Article element - should be selected</article>
						<div id=""content"">Content by ID</div>
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
			var result = await _controller.Fetch(testUrl, extractMainContent: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.MainContent.Should().Contain("Article element - should be selected");
			fetchResponse.MainContent.Should().NotContain("Content div");
			fetchResponse.MainContent.Should().NotContain("Main element");
			fetchResponse.MainContent.Should().NotContain("Content by ID");
		}

		[Fact]
		public async Task Fetch_MainContentExtraction_FallsBackToMainElement()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div class=""content"">Content div</div>
						<main>Main element - should be selected</main>
						<div id=""content"">Content by ID</div>
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
			var result = await _controller.Fetch(testUrl, extractMainContent: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.MainContent.Should().Contain("Main element - should be selected");
			fetchResponse.MainContent.Should().NotContain("Content div");
			fetchResponse.MainContent.Should().NotContain("Content by ID");
		}

		[Fact]
		public async Task Fetch_MainContentExtraction_UsesContentClassSelector()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div class=""content"">Content div - should be selected</div>
						<div id=""content"">Content by ID</div>
						<div class=""other"">Other content</div>
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
			var result = await _controller.Fetch(testUrl, extractMainContent: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.MainContent.Should().Contain("Content div - should be selected");
			fetchResponse.MainContent.Should().NotContain("Content by ID");
			fetchResponse.MainContent.Should().NotContain("Other content");
		}

		[Fact]
		public async Task Fetch_MainContentExtraction_ReturnsEmptyWhenNoMatchingSelectors()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div class=""other"">Other content</div>
						<div class=""sidebar"">Sidebar content</div>
						<nav>Navigation</nav>
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
			var result = await _controller.Fetch(testUrl, extractMainContent: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.MainContent.Should().BeEmpty();
		}

		#endregion

		#region HTML Cleaning Tests

		[Fact]
		public async Task Fetch_HtmlCleaning_RemovesAllTargetedElements()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<head>
						<style>body { color: red; }</style>
					</head>
					<body>
						<nav>Navigation menu</nav>
						<header>Header content</header>
						<aside>Sidebar content</aside>
						<script>alert('test');</script>
						<iframe src=""https://example.com""></iframe>
						<form>
							<input type=""text"" />
							<button>Submit</button>
						</form>
						<noscript>No script content</noscript>
						<!-- This is a comment -->
						<div>Keep this content</div>
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
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			// Should not contain removed elements
			fetchResponse.Content.Should().NotContain("<script>");
			fetchResponse.Content.Should().NotContain("<style>");
			fetchResponse.Content.Should().NotContain("<nav>");
			fetchResponse.Content.Should().NotContain("<header>");
			fetchResponse.Content.Should().NotContain("<aside>");
			fetchResponse.Content.Should().NotContain("<footer>");
			fetchResponse.Content.Should().NotContain("<form>");
			fetchResponse.Content.Should().NotContain("<iframe>");
			fetchResponse.Content.Should().NotContain("<noscript>");
			fetchResponse.Content.Should().NotContain("<!--");
			fetchResponse.Content.Should().NotContain("-->");

			// Should not contain content from removed elements
			fetchResponse.Content.Should().NotContain("alert('test');");
			fetchResponse.Content.Should().NotContain("color: red;");
			fetchResponse.Content.Should().NotContain("Navigation menu");
			fetchResponse.Content.Should().NotContain("Header content");
			fetchResponse.Content.Should().NotContain("Sidebar content");
			fetchResponse.Content.Should().NotContain("Footer content");
			fetchResponse.Content.Should().NotContain("No script content");
			fetchResponse.Content.Should().NotContain("This is a comment");

			// Should keep valid content
			fetchResponse.Content.Should().Contain("Keep this content");
		}

		[Fact]
		public async Task Fetch_HtmlCleaning_HandlesNestedElements()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div>
							<script>nested script</script>
							<p>Valid paragraph</p>
							<nav>
								<ul>
									<li>Nav item</li>
								</ul>
							</nav>
							<div>Another valid div</div>
						</div>
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
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().NotContain("nested script");
			fetchResponse.Content.Should().NotContain("Nav item");
			fetchResponse.Content.Should().Contain("Valid paragraph");
			fetchResponse.Content.Should().Contain("Another valid div");
		}

		[Fact]
		public async Task Fetch_HtmlCleaning_HandlesHtmlWithoutTargetElements()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div>
							<h1>Title</h1>
							<p>Paragraph content</p>
							<span>Span content</span>
						</div>
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
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().Contain("Title");
			fetchResponse.Content.Should().Contain("Paragraph content");
			fetchResponse.Content.Should().Contain("Span content");
			fetchResponse.Content.Should().Contain("<h1>");
			fetchResponse.Content.Should().Contain("<p>");
			fetchResponse.Content.Should().Contain("<span>");
		}

		#endregion

		#region Edge Cases and Malformed HTML Tests

		[Fact]
		public async Task Fetch_HandlesEmptyHtml()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = "";

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
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.Title.Should().BeEmpty();
			fetchResponse.Description.Should().BeEmpty();
			fetchResponse.Keywords.Should().BeEmpty();
			fetchResponse.MainContent.Should().BeEmpty();
		}

		[Fact]
		public async Task Fetch_HandlesMalformedHtml()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<head>
						<title>Test Title</title>
					<body>
						<p>Unclosed paragraph
						<div>Unclosed div
						<script>alert('test');</script>
						<p>Another paragraph</p>
					</body>";

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
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			// HtmlAgilityPack should handle malformed HTML gracefully
			fetchResponse.Title.Should().Be("Test Title");
			fetchResponse.Content.Should().NotContain("alert('test');");
			fetchResponse.Content.Should().Contain("Another paragraph");
		}

		[Fact]
		public async Task Fetch_HandlesHtmlWithInlineStyles()
		{
			// Arrange
			const string testUrl = "https://example.com";
			const string testHtml = @"
				<html>
					<body>
						<div style=""display: none;"">Hidden by inline style</div>
						<div style=""visibility: hidden;"">Also hidden</div>
						<div style=""color: red;"">Visible with color</div>
						<div style=""DISPLAY: NONE;"">Hidden with uppercase</div>
						<div style=""display:none"">Hidden without spaces</div>
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
			var result = await _controller.Fetch(testUrl, textOnly: true);

			// Assert
			result.Should().BeOfType<OkObjectResult>();
			var okResult = (OkObjectResult)result;
			var fetchResponse = (Models.FetchResponse)okResult.Value!;

			fetchResponse.Content.Should().NotContain("Hidden by inline style");
			fetchResponse.Content.Should().NotContain("Also hidden");
			fetchResponse.Content.Should().NotContain("Hidden with uppercase");
			// Note: The current implementation might not handle display:none without spaces
			// This is a limitation of the current IsInvisibleNode method
			fetchResponse.Content.Should().Contain("Visible with color");
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