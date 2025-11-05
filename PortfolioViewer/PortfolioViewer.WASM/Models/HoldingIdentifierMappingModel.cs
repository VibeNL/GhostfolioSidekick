using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class HoldingIdentifierMappingModel
	{
		public required string Symbol { get; set; }
		public required string Name { get; set; }
		public int HoldingId { get; set; }
		public List<PartialIdentifierDisplayModel> PartialIdentifiers { get; set; } = [];
		public List<DataProviderMappingModel> DataProviderMappings { get; set; } = [];
	}

	public class PartialIdentifierDisplayModel
	{
		public required string Identifier { get; set; }
		public List<AssetClass>? AllowedAssetClasses { get; set; }
		public List<AssetSubClass>? AllowedAssetSubClasses { get; set; }
		public List<string> MatchedDataProviders { get; set; } = [];
		public bool HasUnresolvedMapping { get; set; }
		public string? Comment { get; set; }
	}

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

	public class IdentifierMatchingHistoryModel
	{
		public required string PartialIdentifier { get; set; }
		public required string DataSource { get; set; }
		public required string MatchedSymbol { get; set; }
	}
}