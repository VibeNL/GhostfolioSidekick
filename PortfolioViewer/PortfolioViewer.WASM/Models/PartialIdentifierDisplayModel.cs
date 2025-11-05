using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class PartialIdentifierDisplayModel
	{
		public required string Identifier { get; set; }
		public List<AssetClass>? AllowedAssetClasses { get; set; }
		public List<AssetSubClass>? AllowedAssetSubClasses { get; set; }
		public List<string> MatchedDataProviders { get; set; } = [];
		public string? Comment { get; set; }
	}
}