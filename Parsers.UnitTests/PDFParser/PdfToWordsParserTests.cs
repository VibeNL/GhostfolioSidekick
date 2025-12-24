using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.PDFParser
{
	public class PdfToWordsParserTests
	{
		[Fact]
		public void FilterOutFooter_EmptyTokenList_ReturnsEmpty()
		{
			// Arrange
			var tokens = new List<SingleWordToken>();

			// Act
			var result = PdfToWordsParser.FilterOutFooter(tokens);

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void FilterOutFooter_TokensWithoutBoundingBox_ReturnsAllTokens()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				new("text1"),
				new("text2"),
				new("text3")
			};

			// Act
			var result = PdfToWordsParser.FilterOutFooter(tokens);

			// Assert
			Assert.Equal(3, result.Count);
			Assert.All(result, token => Assert.Null(token.BoundingBox));
		}

		[Fact]
		public void FilterOutFooter_SinglePage_FiltersBottomTokens()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				// Top of page (row 10) - should be kept
				new("header", new Position(0, 10, 100)),
				new("content", new Position(0, 50, 100)),
				
				// Middle of page (row 100) - should be kept
				new("middle", new Position(0, 100, 100)),
				
				// Bottom of page (row 190) - within footer threshold, should be filtered
				new("footer", new Position(0, 190, 100)),
				new("pagenumber", new Position(0, 195, 100)) // Max row is 195
			};

			// Act - using default threshold of 50
			var result = PdfToWordsParser.FilterOutFooter(tokens, 50);

			// Assert
			Assert.Equal(3, result.Count);
			Assert.Contains(result, t => t.Text == "header");
			Assert.Contains(result, t => t.Text == "content");
			Assert.Contains(result, t => t.Text == "middle");
			Assert.DoesNotContain(result, t => t.Text == "footer");
			Assert.DoesNotContain(result, t => t.Text == "pagenumber");
		}

		[Fact]
		public void FilterOutFooter_MultiplePages_FiltersBottomTokensPerPage()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				// Page 0 - max row = 100
				new("page0_header", new Position(0, 10, 100)),   // distance = 100-10=90 > 50, keep
				new("page0_content", new Position(0, 40, 100)),  // distance = 100-40=60 > 50, keep  
				new("page0_footer", new Position(0, 90, 100)),   // distance = 100-90=10 <= 50, filter
				new("page0_pagenumber", new Position(0, 100, 100)), // distance = 100-100=0 <= 50, filter
				
				// Page 1 - max row = 150  
				new("page1_header", new Position(1, 5, 100)),    // distance = 150-5=145 > 50, keep
				new("page1_content", new Position(1, 80, 100)),  // distance = 150-80=70 > 50, keep
				new("page1_footer", new Position(1, 140, 100)),  // distance = 150-140=10 <= 50, filter
				new("page1_pagenumber", new Position(1, 150, 100)) // distance = 150-150=0 <= 50, filter
			};

			// Act
			var result = PdfToWordsParser.FilterOutFooter(tokens, 50);

			// Assert - 4 tokens should remain (2 per page)
			Assert.Equal(4, result.Count);
			
			// Page 0 - keep header and content
			Assert.Contains(result, t => t.Text == "page0_header");
			Assert.Contains(result, t => t.Text == "page0_content");
			Assert.DoesNotContain(result, t => t.Text == "page0_footer");
			Assert.DoesNotContain(result, t => t.Text == "page0_pagenumber");
			
			// Page 1 - keep header and content
			Assert.Contains(result, t => t.Text == "page1_header");
			Assert.Contains(result, t => t.Text == "page1_content");
			Assert.DoesNotContain(result, t => t.Text == "page1_footer");
			Assert.DoesNotContain(result, t => t.Text == "page1_pagenumber");
		}

		[Fact]
		public void FilterOutFooter_CustomThreshold_RespectsDifferentThresholds()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				new("content", new Position(0, 50, 100)),
				new("nearbottom", new Position(0, 85, 100)), // 15 units from bottom (max=100)
				new("footer", new Position(0, 100, 100))
			};

			// Act & Assert with threshold 10 - should filter only the very bottom
			var result10 = PdfToWordsParser.FilterOutFooter(tokens, 10);
			Assert.Equal(2, result10.Count);
			Assert.Contains(result10, t => t.Text == "content");
			Assert.Contains(result10, t => t.Text == "nearbottom");
			Assert.DoesNotContain(result10, t => t.Text == "footer");

			// Act & Assert with threshold 20 - should filter both bottom tokens
			var result20 = PdfToWordsParser.FilterOutFooter(tokens, 20);
			Assert.Single(result20);
			Assert.Contains(result20, t => t.Text == "content");
			Assert.DoesNotContain(result20, t => t.Text == "nearbottom");
			Assert.DoesNotContain(result20, t => t.Text == "footer");
		}

		[Fact]
		public void FilterOutFooter_ZeroThreshold_FiltersOnlyExactBottom()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				new("content", new Position(0, 50, 100)),
				new("nearbottom", new Position(0, 99, 100)),
				new("exactbottom", new Position(0, 100, 100)) // Max row
			};

			// Act
			var result = PdfToWordsParser.FilterOutFooter(tokens, 0);

			// Assert - with threshold 0, only tokens at exact max row should be filtered
			Assert.Equal(2, result.Count);
			Assert.Contains(result, t => t.Text == "content");
			Assert.Contains(result, t => t.Text == "nearbottom");
			Assert.DoesNotContain(result, t => t.Text == "exactbottom");
		}

		[Fact]
		public void FilterOutFooter_MixedBoundingBoxes_HandlesNullBoundingBoxes()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				new("no_bbox"), // No bounding box - should be kept
				new("content", new Position(0, 85, 100)), // distance = 100-85=15 > 10, so kept
				new("footer", new Position(0, 95, 100)), // distance = 100-95=5 <= 10, so filtered
				new("exact_footer", new Position(0, 100, 100)) // distance = 100-100=0 <= 10, so filtered
			};

			// Act
			var result = PdfToWordsParser.FilterOutFooter(tokens, 10);

			// Assert - no_bbox and content should remain
			Assert.Equal(2, result.Count);
			Assert.Contains(result, t => t.Text == "no_bbox");
			Assert.Contains(result, t => t.Text == "content");
			Assert.DoesNotContain(result, t => t.Text == "footer");
			Assert.DoesNotContain(result, t => t.Text == "exact_footer");
		}
	}
}