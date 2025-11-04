using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
	/// <summary>
	/// Represents the performance of a holding over a specific time range
	/// </summary>
	public class HoldingTimeRangePerformance
	{
		public string Symbol { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public Money StartPrice { get; set; } = new Money(Currency.USD, 0);
		public Money EndPrice { get; set; } = new Money(Currency.USD, 0);
		public decimal PercentageChange { get; set; }
		public Money AbsoluteChange { get; set; } = new Money(Currency.USD, 0);
		public Money CurrentValue { get; set; } = new Money(Currency.USD, 0);
		public decimal Quantity { get; set; }
	}
}