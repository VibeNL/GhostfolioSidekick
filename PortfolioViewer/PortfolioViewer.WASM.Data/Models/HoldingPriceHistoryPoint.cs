using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	public class HoldingPriceHistoryPoint
	{
		public DateOnly Date { get; set; }
		public Currency Currency { get; set; } = Currency.USD;
		public decimal Price { get; set; }
		public decimal AveragePrice { get; set; }
	}
}