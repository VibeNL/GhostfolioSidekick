using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using GhostfolioSidekick.Parsers.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
	public class ISINParserTests
	{
		private const string ValidIsin = "US0378331005";
		private const string AnotherValidIsin = "DE0005140008";

		private static SingleWordToken Token(string text, int row, int column) =>
			new SingleWordToken(text, new Position(1, row, column));

		[Fact]
		public void ExtractIsin_Tokens_NullInput_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsin((IReadOnlyList<SingleWordToken>)null!);

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_EmptyList_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsin(new List<SingleWordToken>());

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_LineWithIsinPrefix_ValidIsin_ReturnsIsin()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				Token("ISIN:", 1, 0),
				Token(ValidIsin, 1, 1)
			};

			// Act
			var result = ISINParser.ExtractIsin(tokens);

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_LineWithIsinPrefix_InvalidValue_ReturnsEmpty()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				Token("ISIN:", 1, 0),
				Token("NOTVALID", 1, 1)
			};

			// Act
			var result = ISINParser.ExtractIsin(tokens);

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_LineWithIsinPrefixCaseInsensitive_ValidIsin_ReturnsIsin()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				Token("isin:", 1, 0),
				Token(ValidIsin, 1, 1)
			};

			// Act
			var result = ISINParser.ExtractIsin(tokens);

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_StandaloneValidIsin_ReturnsIsin()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				Token(ValidIsin, 1, 0)
			};

			// Act
			var result = ISINParser.ExtractIsin(tokens);

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_NoValidIsin_ReturnsEmpty()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				Token("SomeText", 1, 0),
				Token("MoreText", 1, 1)
			};

			// Act
			var result = ISINParser.ExtractIsin(tokens);

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_IsinOnSecondRow_ReturnsIsin()
		{
			// Arrange
			var tokens = new List<SingleWordToken>
			{
				Token("SomeText", 1, 0),
				Token("ISIN:", 2, 0),
				Token(ValidIsin, 2, 1)
			};

			// Act
			var result = ISINParser.ExtractIsin(tokens);

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_Tokens_MultipleTokensOnSameRow_OrderedByColumn()
		{
			// Arrange: tokens intentionally added out of column order
			var tokens = new List<SingleWordToken>
			{
				Token(ValidIsin, 1, 1),
				Token("ISIN:", 1, 0)
			};

			// Act
			var result = ISINParser.ExtractIsin(tokens);

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_String_NullInput_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsin((string)null!);

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_String_EmptyInput_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsin(string.Empty);

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_String_WhitespaceInput_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsin("   ");

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_String_LineWithIsinPrefix_ValidIsin_ReturnsIsin()
		{
			// Act
			var result = ISINParser.ExtractIsin($"ISIN:{ValidIsin}");

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_String_LineWithIsinPrefixAndSpace_ValidIsin_ReturnsIsin()
		{
			// Act
			var result = ISINParser.ExtractIsin($"ISIN: {ValidIsin}");

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_String_LineWithIsinPrefix_InvalidValue_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsin("ISIN:NOTVALID");

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_String_LineWithIsinPrefixCaseInsensitive_ValidIsin_ReturnsIsin()
		{
			// Act
			var result = ISINParser.ExtractIsin($"isin:{ValidIsin}");

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_String_LineIsValidIsin_ReturnsIsin()
		{
			// Act
			var result = ISINParser.ExtractIsin(ValidIsin);

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsin_String_LineIsNotValidIsin_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsin("NOTANISINXYZ");

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsin_String_LineIsValidIsinWithSurroundingWhitespace_ReturnsIsin()
		{
			// Act
			var result = ISINParser.ExtractIsin($"  {ValidIsin}  ");

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsinMultistring_NullInput_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsinMultistring(null!);

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsinMultistring_EmptyInput_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsinMultistring(string.Empty);

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsinMultistring_WhitespaceInput_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsinMultistring("   ");

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsinMultistring_StringContainingOnlyValidIsin_ReturnsIsin()
		{
			// Act
			var result = ISINParser.ExtractIsinMultistring(ValidIsin);

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsinMultistring_StringWithMultipleWordsIncludingValidIsin_ReturnsIsin()
		{
			// Act
			var result = ISINParser.ExtractIsinMultistring($"SomeText {ValidIsin} MoreText");

			// Assert
			Assert.Equal(ValidIsin, result);
		}

		[Fact]
		public void ExtractIsinMultistring_StringWithNoValidIsin_ReturnsEmpty()
		{
			// Act
			var result = ISINParser.ExtractIsinMultistring("SomeText MoreText NoIsinHere");

			// Assert
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ExtractIsinMultistring_StringWithMultipleValidIsins_ReturnsFirst()
		{
			// Act
			var result = ISINParser.ExtractIsinMultistring($"{ValidIsin} {AnotherValidIsin}");

			// Assert
			Assert.Equal(ValidIsin, result);
		}
	}
}
