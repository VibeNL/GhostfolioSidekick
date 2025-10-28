using GhostfolioSidekick.AI.Functions;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class SearchResultItemTests
	{
		[Fact]
		public void SearchResultItem_ShouldInitializeWithDefaults()
		{
			// Act
			var item = new SearchResultItem();

			// Assert
			Assert.Equal(string.Empty, item.Title);
			Assert.Equal(string.Empty, item.Link);
			Assert.Equal(string.Empty, item.Content);
		}

		[Fact]
		public void SearchResultItem_ShouldAllowSettingProperties()
		{
			// Act
			var item = new SearchResultItem
			{
				Title = "Test Title",
				Link = "https://example.com",
				Content = "Test content here"
			};

			// Assert
			Assert.Equal("Test Title", item.Title);
			Assert.Equal("https://example.com", item.Link);
			Assert.Equal("Test content here", item.Content);
		}
	}
}