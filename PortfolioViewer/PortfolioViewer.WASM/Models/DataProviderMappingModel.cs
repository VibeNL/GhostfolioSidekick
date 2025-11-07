using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class DataProviderMappingModel
	{
		public required string DataSource { get; set; }
		public required string Symbol { get; set; }
		public required string Name { get; set; }
		public string? ISIN { get; set; }
		public AssetClass AssetClass { get; set; }
		public AssetSubClass? AssetSubClass { get; set; }
		public required string Currency { get; set; }
		public List<string> Identifiers { get; set; } = [];
		public List<string> MatchedPartialIdentifiers { get; set; } = [];
	}
}