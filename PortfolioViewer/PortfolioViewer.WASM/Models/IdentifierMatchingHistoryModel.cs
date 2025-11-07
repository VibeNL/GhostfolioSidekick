namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class IdentifierMatchingHistoryModel
	{
		public required string PartialIdentifier { get; set; }
		public required string DataSource { get; set; }
		public required string MatchedSymbol { get; set; }
	}
}