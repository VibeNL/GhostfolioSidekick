using GhostfolioSidekick.AI.Functions;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class SearchResultsTests
	{
		[Fact]
		public void SearchResults_ShouldInitializeWithDefaults()
		{
			// Act
			var searchResults = new SearchResults();

			// Assert
			Assert.Equal(string.Empty, searchResults.Query);
			Assert.NotNull(searchResults.Items);
			Assert.Empty(searchResults.Items);
		}

		[Fact]
		public void SearchResults_ShouldAllowSettingProperties()
		{
			// Arrange
			var items = new List<SearchResultItem>
			{
				new() { Title = "Test Title", Link = "https://test.com", Content = "Test content" }
			};

			// Act
			var searchResults = new SearchResults
			{
				Query = "test query",
				Items = items
			};

			// Assert
			Assert.Equal("test query", searchResults.Query);
			Assert.Single(searchResults.Items);
			Assert.Equal("Test Title", searchResults.Items[0].Title);
		}
	}
}