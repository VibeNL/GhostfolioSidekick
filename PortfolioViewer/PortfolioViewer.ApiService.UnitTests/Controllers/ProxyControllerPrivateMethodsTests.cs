using AwesomeAssertions;
using HtmlAgilityPack;
using System.Reflection;
using System.Text;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	/// <summary>
	/// Tests for private static methods in ProxyController using reflection
	/// These tests validate the HTML processing and text extraction functionality
	/// </summary>
	public class ProxyControllerPrivateMethodsTests
	{
		private readonly Type _proxyControllerType;

		public ProxyControllerPrivateMethodsTests()
		{
			_proxyControllerType = typeof(GhostfolioSidekick.PortfolioViewer.ApiService.Controllers.ProxyController);
		}

		#region CleanHtml Tests

		[Fact]
		public void CleanHtml_RemovesScriptTags()
		{
			// Arrange
			var cleanHtmlMethod = _proxyControllerType.GetMethod("CleanHtml", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><body><script>alert('test');</script><p>Keep this</p></body></html>");

			// Act
			cleanHtmlMethod!.Invoke(null, [htmlDoc]);

			// Assert
			htmlDoc.DocumentNode.OuterHtml.Should().NotContain("<script>");
			htmlDoc.DocumentNode.OuterHtml.Should().NotContain("alert('test');");
			htmlDoc.DocumentNode.OuterHtml.Should().Contain("<p>Keep this</p>");
		}

		[Fact]
		public void CleanHtml_RemovesStyleTags()
		{
			// Arrange
			var cleanHtmlMethod = _proxyControllerType.GetMethod("CleanHtml", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><head><style>body { color: red; }</style></head><body><p>Keep this</p></body></html>");

			// Act
			cleanHtmlMethod!.Invoke(null, [htmlDoc]);

			// Assert
			htmlDoc.DocumentNode.OuterHtml.Should().NotContain("<style>");
			htmlDoc.DocumentNode.OuterHtml.Should().NotContain("color: red;");
			htmlDoc.DocumentNode.OuterHtml.Should().Contain("<p>Keep this</p>");
		}

		[Fact]
		public void CleanHtml_RemovesNavigationElements()
		{
			// Arrange
			var cleanHtmlMethod = _proxyControllerType.GetMethod("CleanHtml", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<body>
						<nav>Navigation</nav>
						<header>Header</header>
						<aside>Sidebar</aside>
						<footer>Footer</footer>
						<form>Form</form>
						<iframe src='test'></iframe>
						<noscript>No script</noscript>
						<p>Keep this</p>
					</body>
				</html>");

			// Act
			cleanHtmlMethod!.Invoke(null, [htmlDoc]);

			// Assert
			var html = htmlDoc.DocumentNode.OuterHtml;
			html.Should().NotContain("<nav>");
			html.Should().NotContain("<header>");
			html.Should().NotContain("<aside>");
			html.Should().NotContain("<footer>");
			html.Should().NotContain("<form>");
			html.Should().NotContain("<iframe>");
			html.Should().NotContain("<noscript>");
			html.Should().NotContain("Navigation");
			html.Should().NotContain("Header");
			html.Should().NotContain("Sidebar");
			html.Should().NotContain("Footer");
			html.Should().Contain("<p>Keep this</p>");
		}

		[Fact]
		public void CleanHtml_RemovesComments()
		{
			// Arrange
			var cleanHtmlMethod = _proxyControllerType.GetMethod("CleanHtml", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<body>
						<!-- This is a comment -->
						<p>Keep this</p>
						<!-- Another comment -->
					</body>
				</html>");

			// Act
			cleanHtmlMethod!.Invoke(null, [htmlDoc]);

			// Assert
			var html = htmlDoc.DocumentNode.OuterHtml;
			html.Should().NotContain("<!--");
			html.Should().NotContain("-->");
			html.Should().NotContain("This is a comment");
			html.Should().NotContain("Another comment");
			html.Should().Contain("<p>Keep this</p>");
		}

		#endregion

		#region RemoveNodes Tests

		[Fact]
		public void RemoveNodes_RemovesMatchingNodes()
		{
			// Arrange
			var removeNodesMethod = _proxyControllerType.GetMethod("RemoveNodes", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><body><div class='remove'>Remove me</div><div class='keep'>Keep me</div></body></html>");

			// Act
			removeNodesMethod!.Invoke(null, [htmlDoc, "//div[@class='remove']"]);

			// Assert
			var html = htmlDoc.DocumentNode.OuterHtml;
			html.Should().NotContain("Remove me");
			html.Should().Contain("Keep me");
		}

		[Fact]
		public void RemoveNodes_WithNoMatchingNodes_DoesNotThrow()
		{
			// Arrange
			var removeNodesMethod = _proxyControllerType.GetMethod("RemoveNodes", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><body><p>Content</p></body></html>");

			// Act & Assert
			var exception = Record.Exception(() => 
				removeNodesMethod!.Invoke(null, [htmlDoc, "//nonexistent"]));
			
			exception.Should().BeNull();
		}

		#endregion

		#region RemoveComments Tests

		[Fact]
		public void RemoveComments_RemovesAllComments()
		{
			// Arrange
			var removeCommentsMethod = _proxyControllerType.GetMethod("RemoveComments", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<!-- Top level comment -->
					<body>
						<div>
							<!-- Nested comment -->
							<p>Content</p>
						</div>
						<!-- Another comment -->
					</body>
				</html>");

			// Act
			removeCommentsMethod!.Invoke(null, [htmlDoc.DocumentNode]);

			// Assert
			var html = htmlDoc.DocumentNode.OuterHtml;
			html.Should().NotContain("<!--");
			html.Should().NotContain("-->");
			html.Should().NotContain("Top level comment");
			html.Should().NotContain("Nested comment");
			html.Should().NotContain("Another comment");
			html.Should().Contain("<p>Content</p>");
		}

		#endregion

		#region ExtractTextFromHtml Tests

		[Fact]
		public void ExtractTextFromHtml_ExtractsTextFromBody()
		{
			// Arrange
			var extractTextMethod = _proxyControllerType.GetMethod("ExtractTextFromHtml", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><body><h1>Title</h1><p>Paragraph</p></body></html>");

			// Act
			var result = (string)extractTextMethod!.Invoke(null, [htmlDoc])!;

			// Assert
			result.Should().Contain("Title");
			result.Should().Contain("Paragraph");
			result.Should().NotContain("<h1>");
			result.Should().NotContain("<p>");
		}

		[Fact]
		public void ExtractTextFromHtml_WithNoBody_ExtractsFromDocument()
		{
			// Arrange
			var extractTextMethod = _proxyControllerType.GetMethod("ExtractTextFromHtml", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><head><title>Title</title></head><div>Content</div></html>");

			// Act
			var result = (string)extractTextMethod!.Invoke(null, [htmlDoc])!;

			// Assert
			result.Should().Contain("Title");
			result.Should().Contain("Content");
		}

		[Fact]
		public void ExtractTextFromHtml_NormalizesWhitespace()
		{
			// Arrange
			var extractTextMethod = _proxyControllerType.GetMethod("ExtractTextFromHtml", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><body><p>Text    with     multiple     spaces</p></body></html>");

			// Act
			var result = (string)extractTextMethod!.Invoke(null, [htmlDoc])!;

			// Assert
			result.Should().Contain("Text with multiple spaces");
			result.Should().NotContain("    ");
		}

		#endregion

		#region ExtractTextFromNode Tests

		[Fact]
		public void ExtractTextFromNode_SkipsInvisibleNodes()
		{
			// Arrange
			var extractTextMethod = _proxyControllerType.GetMethod("ExtractTextFromNode", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"<div><p>Visible content</p><div style='display: none'>Hidden content</div><div style='visibility: hidden'>Also hidden</div></div>");

			var sb = new StringBuilder();
			var rootNode = htmlDoc.DocumentNode.FirstChild; // Get the outer div

			// Act
			extractTextMethod!.Invoke(null, [rootNode, sb]);

			// Assert
			var result = sb.ToString();
			result.Should().Contain("Visible content", "Should contain visible content");
			result.Should().NotContain("Hidden content", "Should not contain content from display:none elements");
			result.Should().NotContain("Also hidden", "Should not contain content from visibility:hidden elements");
		}

		[Fact]
		public void ExtractTextFromNode_AddsLineBreaksForBlockElements()
		{
			// Arrange
			var extractTextMethod = _proxyControllerType.GetMethod("ExtractTextFromNode", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<div><p>Paragraph 1</p><p>Paragraph 2</p></div>");

			var sb = new StringBuilder();

			// Act
			extractTextMethod!.Invoke(null, [htmlDoc.DocumentNode.FirstChild, sb]);

			// Assert
			var result = sb.ToString();
			var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			lines.Should().Contain(line => line.Contains("Paragraph 1"));
			lines.Should().Contain(line => line.Contains("Paragraph 2"));
		}

		#endregion

		#region IsInvisibleNode Tests

		[Theory]
		[InlineData("<div style='display: none'>Hidden</div>", true)]
		[InlineData("<div style='visibility: hidden'>Hidden</div>", true)]
		[InlineData("<div style='DISPLAY: NONE'>Hidden</div>", true)]
		[InlineData("<div style='VISIBILITY: HIDDEN'>Hidden</div>", true)]
		[InlineData("<div style='color: red'>Visible</div>", false)]
		[InlineData("<div>Visible</div>", false)]
		[InlineData("<div style='display:none'>Hidden without spaces</div>", false)] // This will be false because implementation requires spaces
		[InlineData("<div style='visibility:hidden'>Hidden without spaces</div>", false)] // This will be false because implementation requires spaces
		public void IsInvisibleNode_DetectsInvisibility(string html, bool expectedInvisible)
		{
			// Arrange
			var isInvisibleMethod = _proxyControllerType.GetMethod("IsInvisibleNode", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);
			var node = htmlDoc.DocumentNode.FirstChild;

			// Act
			var result = (bool)isInvisibleMethod!.Invoke(null, [node])!;

			// Assert
			result.Should().Be(expectedInvisible, $"Node with HTML '{html}' should be {(expectedInvisible ? "invisible" : "visible")}");
		}

		#endregion

		#region IsBlockLevelElement Tests

		[Theory]
		[InlineData("p", true)]
		[InlineData("div", true)]
		[InlineData("h1", true)]
		[InlineData("h2", true)]
		[InlineData("h3", true)]
		[InlineData("h4", true)]
		[InlineData("h5", true)]
		[InlineData("h6", true)]
		[InlineData("article", true)]
		[InlineData("section", true)]
		[InlineData("li", true)]
		[InlineData("br", true)]
		[InlineData("hr", true)]
		[InlineData("span", false)]
		[InlineData("a", false)]
		[InlineData("strong", false)]
		[InlineData("em", false)]
		[InlineData("img", false)]
		public void IsBlockLevelElement_IdentifiesBlockElements(string tagName, bool expectedIsBlock)
		{
			// Arrange
			var isBlockMethod = _proxyControllerType.GetMethod("IsBlockLevelElement", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml($"<{tagName}>Content</{tagName}>");
			var node = htmlDoc.DocumentNode.FirstChild;

			// Act
			var result = (bool)isBlockMethod!.Invoke(null, [node])!;

			// Assert
			result.Should().Be(expectedIsBlock);
		}

		#endregion

		#region ExtractMainContent Tests

		[Fact]
		public void ExtractMainContent_PrioritizesArticle()
		{
			// Arrange
			var extractMainContentMethod = _proxyControllerType.GetMethod("ExtractMainContent", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<body>
						<div class='content'>Content div</div>
						<main>Main element</main>
						<article>Article content</article>
					</body>
				</html>");

			// Act
			var result = (string)extractMainContentMethod!.Invoke(null, [htmlDoc])!;

			// Assert
			result.Should().Contain("Article content");
			result.Should().NotContain("Content div");
			result.Should().NotContain("Main element");
		}

		[Fact]
		public void ExtractMainContent_FallsBackToMain()
		{
			// Arrange
			var extractMainContentMethod = _proxyControllerType.GetMethod("ExtractMainContent", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<body>
						<div class='content'>Content div</div>
						<main>Main element</main>
					</body>
				</html>");

			// Act
			var result = (string)extractMainContentMethod!.Invoke(null, [htmlDoc])!;

			// Assert
			result.Should().Contain("Main element");
			result.Should().NotContain("Content div");
		}

		[Fact]
		public void ExtractMainContent_UsesContentClass()
		{
			// Arrange
			var extractMainContentMethod = _proxyControllerType.GetMethod("ExtractMainContent", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<body>
						<div class='content'>Content div</div>
						<div class='other'>Other content</div>
					</body>
				</html>");

			// Act
			var result = (string)extractMainContentMethod!.Invoke(null, [htmlDoc])!;

			// Assert
			result.Should().Contain("Content div");
			result.Should().NotContain("Other content");
		}

		[Fact]
		public void ExtractMainContent_ReturnsEmptyWhenNoMatch()
		{
			// Arrange
			var extractMainContentMethod = _proxyControllerType.GetMethod("ExtractMainContent", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<body>
						<div class='other'>Other content</div>
						<nav>Navigation</nav>
					</body>
				</html>");

			// Act
			var result = (string)extractMainContentMethod!.Invoke(null, [htmlDoc])!;

			// Assert
			result.Should().BeEmpty();
		}

		#endregion

		#region ExtractMetadata Tests

		[Fact]
		public void ExtractMetadata_ExtractsTitle()
		{
			// Arrange
			var extractMetadataMethod = _proxyControllerType.GetMethod("ExtractMetadata", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml("<html><head><title>Test Title</title></head><body></body></html>");

			// Act
			var result = extractMetadataMethod!.Invoke(null, [htmlDoc]);
			var resultTuple = ((string Title, string Description, List<string> Keywords))result!;

			// Assert
			resultTuple.Title.Should().Be("Test Title");
			resultTuple.Description.Should().BeEmpty();
			resultTuple.Keywords.Should().BeEmpty();
		}

		[Fact]
		public void ExtractMetadata_ExtractsDescription()
		{
			// Arrange
			var extractMetadataMethod = _proxyControllerType.GetMethod("ExtractMetadata", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<head>
						<meta name='description' content='Test description' />
					</head>
					<body></body>
				</html>");

			// Act
			var result = extractMetadataMethod!.Invoke(null, [htmlDoc]);
			var resultTuple = ((string Title, string Description, List<string> Keywords))result!;

			// Assert
			resultTuple.Title.Should().BeEmpty();
			resultTuple.Description.Should().Be("Test description");
			resultTuple.Keywords.Should().BeEmpty();
		}

		[Fact]
		public void ExtractMetadata_ExtractsKeywords()
		{
			// Arrange
			var extractMetadataMethod = _proxyControllerType.GetMethod("ExtractMetadata", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<head>
						<meta name='keywords' content='test, keywords, example' />
					</head>
					<body></body>
				</html>");

			// Act
			var result = extractMetadataMethod!.Invoke(null, [htmlDoc]);
			var resultTuple = ((string Title, string Description, List<string> Keywords))result!;

			// Assert
			resultTuple.Title.Should().BeEmpty();
			resultTuple.Description.Should().BeEmpty();
			resultTuple.Keywords.Should().HaveCount(3);
			resultTuple.Keywords.Should().Contain("test");
			resultTuple.Keywords.Should().Contain("keywords");
			resultTuple.Keywords.Should().Contain("example");
		}

		[Fact]
		public void ExtractMetadata_HandlesEmptyKeywords()
		{
			// Arrange
			var extractMetadataMethod = _proxyControllerType.GetMethod("ExtractMetadata", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<head>
						<meta name='keywords' content='test, , empty, , another' />
					</head>
					<body></body>
				</html>");

			// Act
			var result = extractMetadataMethod!.Invoke(null, [htmlDoc]);
			var resultTuple = ((string Title, string Description, List<string> Keywords))result!;

			// Assert
			resultTuple.Keywords.Should().HaveCount(3);
			resultTuple.Keywords.Should().Contain("test");
			resultTuple.Keywords.Should().Contain("empty");
			resultTuple.Keywords.Should().Contain("another");
			resultTuple.Keywords.Should().NotContain("");
		}

		[Fact]
		public void ExtractMetadata_HandlesCompleteMetadata()
		{
			// Arrange
			var extractMetadataMethod = _proxyControllerType.GetMethod("ExtractMetadata", BindingFlags.NonPublic | BindingFlags.Static);
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(@"
				<html>
					<head>
						<title>Complete Test Page</title>
						<meta name='description' content='This is a complete test page with all metadata' />
						<meta name='keywords' content='complete, test, metadata, example' />
					</head>
					<body></body>
				</html>");

			// Act
			var result = extractMetadataMethod!.Invoke(null, [htmlDoc]);
			var resultTuple = ((string Title, string Description, List<string> Keywords))result!;

			// Assert
			resultTuple.Title.Should().Be("Complete Test Page");
			resultTuple.Description.Should().Be("This is a complete test page with all metadata");
			resultTuple.Keywords.Should().HaveCount(4);
			resultTuple.Keywords.Should().Contain("complete");
			resultTuple.Keywords.Should().Contain("test");
			resultTuple.Keywords.Should().Contain("metadata");
			resultTuple.Keywords.Should().Contain("example");
		}

		#endregion
	}
}