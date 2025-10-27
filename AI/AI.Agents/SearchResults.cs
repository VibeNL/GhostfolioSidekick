namespace GhostfolioSidekick.AI.Agents
{
	public class SearchResults
	{
		public string Query { get; set; } = string.Empty;
		public List<SearchResultItem> Items { get; set; } = [];
	}
}
