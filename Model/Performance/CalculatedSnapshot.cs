using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Model.Performance
{
	public record CalculatedSnapshot(DateOnly Date, decimal Quantity, Money AverageCostPrice, Money CurrentUnitPrice, Money TotalInvested, Money TotalValue)
	{
		public static CalculatedSnapshot Empty(Currency currency) => new(DateOnly.MinValue, 0, Money.Zero(currency), Money.Zero(currency), Money.Zero(currency), Money.Zero(currency));
	}
}