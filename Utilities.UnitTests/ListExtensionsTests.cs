namespace GhostfolioSidekick.Utilities.UnitTests
{
	public class ListExtensionsTests
	{
		#region FilterEmpty Tests

		[Fact]
		public void FilterEmpty_WithNullItems_ShouldRemoveNulls()
		{
			// Arrange
			var list = new List<string?> { "apple", null, "banana", null, "cherry" };

			// Act
			var result = list.FilterEmpty();

			// Assert
			Assert.Equal(3, result.Count);
			Assert.Contains("apple", result);
			Assert.Contains("banana", result);
			Assert.Contains("cherry", result);
			Assert.DoesNotContain(null, result);
		}

		[Fact]
		public void FilterEmpty_WithEmptyList_ShouldReturnEmptyList()
		{
			// Arrange
			var list = new List<string?>();

			// Act
			var result = list.FilterEmpty();

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void FilterEmpty_WithAllNulls_ShouldReturnEmptyList()
		{
			// Arrange
			var list = new List<string?> { null, null, null };

			// Act
			var result = list.FilterEmpty();

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void FilterEmpty_WithNoNulls_ShouldReturnAllItems()
		{
			// Arrange
			var list = new List<string?> { "apple", "banana", "cherry" };

			// Act
			var result = list.FilterEmpty();

			// Assert
			Assert.Equal(3, result.Count);
			Assert.Contains("apple", result);
			Assert.Contains("banana", result);
			Assert.Contains("cherry", result);
		}

		[Fact]
		public void FilterEmpty_WithCustomObjects_ShouldFilterCorrectly()
		{
			// Arrange
			var obj1 = new object();
			var obj2 = new object();
			var list = new List<object?> { obj1, null, obj2, null };

			// Act
			var result = list.FilterEmpty();

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains(obj1, result);
			Assert.Contains(obj2, result);
		}

		#endregion

		#region FilterInvalidNames Tests

		[Fact]
		public void FilterInvalidNames_WithValidNames_ShouldReturnAllNames()
		{
			// Arrange
			var names = new List<string?> { "Apple", "Microsoft", "Tesla", "Amazon" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(4, result.Count);
			Assert.Contains("Apple", result);
			Assert.Contains("Microsoft", result);
			Assert.Contains("Tesla", result);
			Assert.Contains("Amazon", result);
		}

		[Fact]
		public void FilterInvalidNames_WithCorporateSuffixes_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { "Inc", "Corp", "LLC", "Ltd", "Company", "Apple" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Single(result);
			Assert.Contains("Apple", result);
			Assert.DoesNotContain("Inc", result);
			Assert.DoesNotContain("Corp", result);
			Assert.DoesNotContain("LLC", result);
			Assert.DoesNotContain("Ltd", result);
			Assert.DoesNotContain("Company", result);
		}

		[Fact]
		public void FilterInvalidNames_WithCommonWords_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { "The", "And", "Of", "For", "Apple", "Microsoft" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains("Apple", result);
			Assert.Contains("Microsoft", result);
			Assert.DoesNotContain("The", result);
			Assert.DoesNotContain("And", result);
			Assert.DoesNotContain("Of", result);
			Assert.DoesNotContain("For", result);
		}

		[Fact]
		public void FilterInvalidNames_WithCaseInsensitive_ShouldFilterCorrectly()
		{
			// Arrange
			var names = new List<string?> { "inc", "CORP", "Llc", "the", "Apple", "microsoft" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains("Apple", result);
			Assert.Contains("microsoft", result);
		}

		[Fact]
		public void FilterInvalidNames_WithNullAndEmptyStrings_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { null, "", "  ", "Apple", "Microsoft" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains("Apple", result);
			Assert.Contains("Microsoft", result);
		}

		[Fact]
		public void FilterInvalidNames_WithSingleCharacters_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { "A", "B", "C", "Apple", "Microsoft" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains("Apple", result);
			Assert.Contains("Microsoft", result);
			Assert.DoesNotContain("A", result);
			Assert.DoesNotContain("B", result);
			Assert.DoesNotContain("C", result);
		}

		[Fact]
		public void FilterInvalidNames_WithWhitespace_ShouldTrimAndFilter()
		{
			// Arrange
			var names = new List<string?> { "  Apple  ", " Inc ", "  Microsoft  ", "   Corp   " };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains("Apple", result);
			Assert.Contains("Microsoft", result);
		}

		[Fact]
		public void FilterInvalidNames_WithDuplicates_ShouldReturnDistinct()
		{
			// Arrange
			var names = new List<string?> { "Apple", "Microsoft", "Apple", "Tesla", "Microsoft" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(3, result.Count);
			Assert.Contains("Apple", result);
			Assert.Contains("Microsoft", result);
			Assert.Contains("Tesla", result);
		}

		[Fact]
		public void FilterInvalidNames_WithEmptyList_ShouldReturnEmptyList()
		{
			// Arrange
			var names = new List<string?>();

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void FilterInvalidNames_WithAllInvalidNames_ShouldReturnEmptyList()
		{
			// Arrange
			var names = new List<string?> { "Inc", "Corp", "The", "And", "A", null, "" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void FilterInvalidNames_WithInternationalCorporateSuffixes_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { "SA", "AG", "GmbH", "SE", "NV", "BV", "Plc", "Apple" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Single(result);
			Assert.Contains("Apple", result);
		}

		[Fact]
		public void FilterInvalidNames_WithVariationsOfCorporateTerms_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { "Inc.", "Corp.", "L.L.C.", "L.P.", "L.L.P.", "P.L.C.", "Apple" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Single(result);
			Assert.Contains("Apple", result);
		}

		[Fact]
		public void FilterInvalidNames_WithVerbsAndCommonWords_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { "Is", "Was", "Are", "Were", "Be", "Been", "Being", "Have", "Has", "Had", "Apple" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Single(result);
			Assert.Contains("Apple", result);
		}

		[Fact]
		public void FilterInvalidNames_WithModalVerbs_ShouldFilterThem()
		{
			// Arrange
			var names = new List<string?> { "Will", "Would", "Could", "Should", "May", "Might", "Must", "Can", "Cannot", "Apple" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Single(result);
			Assert.Contains("Apple", result);
		}

		[Fact]
		public void FilterInvalidNames_PreservesOrderOfValidItems()
		{
			// Arrange
			var names = new List<string?> { "Zebra", "Inc", "Apple", "Corp", "Microsoft" };

			// Act
			var result = names.FilterInvalidNames();

			// Assert
			Assert.Equal(3, result.Count);
			Assert.Equal("Zebra", result[0]);
			Assert.Equal("Apple", result[1]);
			Assert.Equal("Microsoft", result[2]);
		}

		#endregion

		#region TrimSymbolSuffixes Tests
		[Theory]
		[InlineData(new string[] { "SDIV.L" }, new string[] { "SDIV" })]
		[InlineData(new string[] { "AAPL.DE", "MSFT.AS", "GOOG" }, new string[] { "AAPL", "MSFT", "GOOG" })]
		[InlineData(new string[] { "TSLA", "TSLA.PA" }, new string[] { "TSLA" })]
		[InlineData(new string[] { "VUSA.L", "VUSA" }, new string[] { "VUSA" })]
		[InlineData(new string[] { "BRK.B" }, new string[] { "BRK" })]
		[InlineData(new string[] { "AAPL" }, new string[] { "AAPL" })]
		[InlineData(new string[] { "AAPL.DE", "AAPL.DE" }, new string[] { "AAPL" })]
		public void TrimSymbolSuffixes_RemovesSuffixesAndReturnsDistinct(string[] input, string[] expected)
		{
			// Act
			var result = input.TrimSymbolSuffixes();

			// Assert
			Assert.Equal(expected, result);
		}

		[Fact]
		public void TrimSymbolSuffixes_RemovesDotSuffixes()
		{
			var list = new List<string?> { "AAPL.DE", "MSFT.AS", "GOOG.DE", "TSLA.AS" };
			var result = list.TrimSymbolSuffixes();
			Assert.Contains("AAPL", result);
			Assert.Contains("MSFT", result);
			Assert.Contains("GOOG", result);
			Assert.Contains("TSLA", result);
			Assert.DoesNotContain("AAPL.DE", result);
			Assert.DoesNotContain("MSFT.AS", result);
		}

		[Fact]
		public void TrimSymbolSuffixes_LeavesNonSuffixedSymbolsUnchanged()
		{
			var list = new List<string?> { "AAPL", "MSFT", "GOOG" };
			var result = list.TrimSymbolSuffixes();
			Assert.Equal(list.Select(x => x!).ToList(), result);
		}

		[Fact]
		public void TrimSymbolSuffixes_FiltersNullAndEmpty()
		{
			var list = new List<string?> { null, "", "AAPL.DE", "GOOG" };
			var result = list.TrimSymbolSuffixes();
			Assert.Contains("AAPL", result);
			Assert.Contains("GOOG", result);
			Assert.DoesNotContain(null, result);
			Assert.DoesNotContain("", result);
		}

		[Fact]
		public void TrimSymbolSuffixes_ReturnsDistinctCaseInsensitive()
		{
			var list = new List<string?> { "AAPL.DE", "aapl.de", "AAPL", "aapl" };
			var result = list.TrimSymbolSuffixes();
			Assert.Single(result);
			Assert.Equal("AAPL", result[0], ignoreCase: true);
		}

		[Fact]
        public void TrimSymbolSuffixes_HandlesShortStringsAndNonMatchingSuffix()
		{
			var list = new List<string?> { "DE", "AS", "A.DE", "B.AS", ".DE", ".AS", "AAPL.DEX", "MSFT.ASX" };
			var result = list.TrimSymbolSuffixes();
			Assert.Contains("DE", result);
			Assert.Contains("AS", result);
			Assert.Contains("A", result); // "A.DE" becomes "A"
			Assert.Contains("B", result); // "B.AS" becomes "B"
			Assert.Contains(".DE", result);
			Assert.Contains(".AS", result);
			Assert.Contains("AAPL", result); // "AAPL.DEX" becomes "AAPL"
			Assert.Contains("MSFT", result); // "MSFT.ASX" becomes "MSFT"
		}

		#endregion
	}
}