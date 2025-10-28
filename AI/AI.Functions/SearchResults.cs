namespace GhostfolioSidekick.AI.Functions
{
	public class SearchResults
	{
		public string Query { get; set; } = string.Empty;
		public List<SearchResultItem> Items { get; set; } = [];
	}
}
