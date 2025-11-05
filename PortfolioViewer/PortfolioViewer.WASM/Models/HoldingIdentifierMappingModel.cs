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
}