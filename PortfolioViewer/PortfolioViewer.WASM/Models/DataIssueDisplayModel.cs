using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	public class DataIssueDisplayModel
	{
		public string IssueType { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public DateTime Date { get; set; }
		public string AccountName { get; set; } = string.Empty;
		public string ActivityType { get; set; } = string.Empty;
		public string? Symbol { get; set; }
		public string? SymbolIdentifiers { get; set; }
		public decimal? Quantity { get; set; }
		public Money? UnitPrice { get; set; }
		public Money? Amount { get; set; }
		public string TransactionId { get; set; } = string.Empty;
		public string? ActivityDescription { get; set; }
		public string Severity { get; set; } = "Warning"; // Warning, Error, Info
	}
}