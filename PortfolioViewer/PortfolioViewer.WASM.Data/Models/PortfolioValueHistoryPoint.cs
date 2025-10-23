namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	public class PortfolioValueHistoryPoint
	{
		public DateOnly Date { get; set; }

		public decimal Value { get; set; }

		public decimal Invested { get; set; }
	}
}