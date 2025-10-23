using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	public class HoldingPriceHistoryPoint
	{
		public DateOnly Date { get; set; }
		public Money Price { get; set; } = new Money(Currency.USD, 0);
		public Money AveragePrice { get; set; } = new Money(Currency.USD, 0);
	}
}