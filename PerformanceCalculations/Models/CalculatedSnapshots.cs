using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PerformanceCalculations.Models
{
	public record CalculatedSnapshots
	{
		public DateOnly Date { get; set; }

		public decimal Quantity { get; set; } = 0;

		public Money AverageCostPrice { get; set; } = Money.Zero;

		public Money TotalInvested { get; set; } = Money.Zero;

		public Money TotalValue { get; set; } = Money.Zero;
	}
}