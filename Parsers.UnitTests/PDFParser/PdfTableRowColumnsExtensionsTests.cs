using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.PDFParser
{
	public class PdfTableRowColumnsExtensionsTests
	{
		[Fact]
		public void GetColumnValue_NullRowColumns_ReturnsNull()
		{
			// Arrange
			PdfTableRowColumns? rowColumns = null;
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "Name");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetColumnValue_NullHeader_ReturnsNull()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John", "100", "2024-01-01"]);
			PdfTableRow? header = null;

			// Act
			var result = rowColumns.GetColumnValue(header, "Name");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetColumnValue_NullColumnName_ReturnsNull()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John", "100", "2024-01-01"]);
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, null);

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetColumnValue_EmptyColumnName_ReturnsNull()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John", "100", "2024-01-01"]);
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetColumnValue_ExactMatch_ReturnsCorrectValue()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John Doe", "150.50", "2024-01-15"]);
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "Amount");

			// Assert
			Assert.Equal("150.50", result);
		}

		[Fact]
		public void GetColumnValue_CaseInsensitiveMatch_ReturnsCorrectValue()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John Doe", "150.50", "2024-01-15"]);
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "amount");

			// Assert
			Assert.Equal("150.50", result);
		}

		[Fact]
		public void GetColumnValue_PartialMatch_ReturnsCorrectValue()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John Doe", "150.50", "2024-01-15"]);
			var header = CreatePdfTableRow(["Customer Name", "Total Amount", "Transaction Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "Total");

			// Assert
			Assert.Equal("150.50", result);
		}

		[Fact]
		public void GetColumnValue_MultipleWordsInColumnName_ReturnsCorrectValue()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John Doe", "150.50", "2024-01-15"]);
			var header = CreatePdfTableRow(["Customer Name", "Total Amount", "Transaction Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "Customer Name");

			// Assert
			Assert.Equal("John Doe", result);
		}

		[Fact]
		public void GetColumnValue_PartialWordsMatch_ReturnsCorrectValue()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John Doe", "150.50", "2024-01-15"]);
			var header = CreatePdfTableRow(["Customer Name", "Total Amount USD", "Transaction Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "Amount USD");

			// Assert
			Assert.Equal("150.50", result);
		}

		[Fact]
		public void GetColumnValue_NoMatch_ReturnsNull()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John Doe", "150.50", "2024-01-15"]);
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "NonExistentColumn");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetColumnValue_IndexOutOfRange_ReturnsNull()
		{
			// Arrange
			var rowColumns = CreatePdfTableRowColumns(["John", "150.50"]); // Only 2 columns
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]); // 3 headers

			// Act
			var result = rowColumns.GetColumnValue(header, "Date");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetColumnValue_EmptyColumnTokens_ReturnsNull()
		{
			// Arrange
			var tokens1 = new List<SingleWordToken> { new("John") };
			var tokens2 = new List<SingleWordToken>(); // Empty column
			var tokens3 = new List<SingleWordToken> { new("2024-01-15") };
			
			var columns = new List<IReadOnlyList<SingleWordToken>> { tokens1, tokens2, tokens3 };
			var rowColumns = new PdfTableRowColumns(["Name", "Amount", "Date"], 0, 1, columns);
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var result = rowColumns.GetColumnValue(header, "Amount");

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void GetColumnValue_MultipleTokensInColumn_JoinsWithSpace()
		{
			// Arrange
			var tokens1 = new List<SingleWordToken> { new("John"), new("Doe") };
			var tokens2 = new List<SingleWordToken> { new("150.50") };
			var tokens3 = new List<SingleWordToken> { new("Jan"), new("15,"), new("2024") };
			
			var columns = new List<IReadOnlyList<SingleWordToken>> { tokens1, tokens2, tokens3 };
			var rowColumns = new PdfTableRowColumns(["Name", "Amount", "Date"], 0, 1, columns);
			var header = CreatePdfTableRow(["Name", "Amount", "Date"]);

			// Act
			var resultName = rowColumns.GetColumnValue(header, "Name");
			var resultDate = rowColumns.GetColumnValue(header, "Date");

			// Assert
			Assert.Equal("John Doe", resultName);
			Assert.Equal("Jan 15, 2024", resultDate);
		}

		[Fact]
		public void ExtractHeaderKeywords_NullHeader_ReturnsEmptyArray()
		{
			// Arrange
			PdfTableRow? header = null;

			// Act
			var result = CallExtractHeaderKeywords(header);

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void ExtractHeaderKeywords_HeaderWithNullTokens_ReturnsEmptyArray()
		{
			// Arrange
			var header = new PdfTableRow(["Name"], 0, 0, null!);

			// Act
			var result = CallExtractHeaderKeywords(header);

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void ExtractHeaderKeywords_TokensWithoutBoundingBox_FiltersOut()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				new("Name"), // No bounding box
				new("Amount", new Position(0, 0, 100)),
				new("Date") // No bounding box
			};
			var header = new PdfTableRow(["Name", "Amount", "Date"], 0, 0, tokens);

			// Act
			var result = CallExtractHeaderKeywords(header);

			// Assert
			Assert.Single(result);
			Assert.Equal("Amount", result[0]);
		}

		[Fact]
		public void ExtractHeaderKeywords_TokensGroupedByColumn_MergesCorrectly()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				new("Customer", new Position(0, 0, 10)),
				new("Name", new Position(0, 0, 15)), // Close to previous token
				new("Total", new Position(0, 0, 100)),
				new("Amount", new Position(0, 0, 105)), // Close to previous token
				new("Date", new Position(0, 0, 200))
			};
			var header = new PdfTableRow(["Customer Name", "Total Amount", "Date"], 0, 0, tokens);

			// Act
			var result = CallExtractHeaderKeywords(header);

			// Assert
			Assert.Equal(3, result.Length);
			Assert.Equal("Customer Name", result[0]);
			Assert.Equal("Total Amount", result[1]);
			Assert.Equal("Date", result[2]);
		}

		[Fact]
		public void ExtractHeaderKeywords_TokensFarApart_SeparatedCorrectly()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				new("Name", new Position(0, 0, 10)),
				new("Amount", new Position(0, 0, 100)), // Far from previous (> 5 units)
				new("Date", new Position(0, 0, 200)) // Far from previous
			};
			var header = new PdfTableRow(["Name", "Amount", "Date"], 0, 0, tokens);

			// Act
			var result = CallExtractHeaderKeywords(header);

			// Assert
			Assert.Equal(3, result.Length);
			Assert.Equal("Name", result[0]);
			Assert.Equal("Amount", result[1]);
			Assert.Equal("Date", result[2]);
		}

		[Fact]
		public void ExtractHeaderKeywords_TokensOrderedByColumn_MaintainsOrder()
		{
			// Arrange - tokens provided out of column order
			var tokens = new List<SingleWordToken>
			{
				new("Date", new Position(0, 0, 200)),
				new("Name", new Position(0, 0, 10)),
				new("Amount", new Position(0, 0, 100))
			};
			var header = new PdfTableRow(["Name", "Amount", "Date"], 0, 0, tokens);

			// Act
			var result = CallExtractHeaderKeywords(header);

			// Assert - should be ordered by column position
			Assert.Equal(3, result.Length);
			Assert.Equal("Name", result[0]);
			Assert.Equal("Amount", result[1]);
			Assert.Equal("Date", result[2]);
		}

		// Helper methods
		private static PdfTableRowColumns CreatePdfTableRowColumns(string[] values)
		{
			var columns = values.Select(value => (IReadOnlyList<SingleWordToken>)new List<SingleWordToken> { new(value) }).ToList();
			return new PdfTableRowColumns(["Header1", "Header2", "Header3"], 0, 1, columns);
		}

		private static PdfTableRow CreatePdfTableRow(string[] headers)
		{
			var tokens = headers.Select((header, index) => new SingleWordToken(header, new Position(0, 0, index * 100))).ToList();
			return new PdfTableRow(headers, 0, 0, tokens);
		}

		// Use reflection to call the private ExtractHeaderKeywords method for testing
		private static string[] CallExtractHeaderKeywords(PdfTableRow? header)
		{
			var method = typeof(PdfTableRowColumnsExtensions).GetMethod("ExtractHeaderKeywords",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			
			return (string[])method!.Invoke(null, [header])!;
		}
	}
}