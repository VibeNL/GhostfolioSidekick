using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Activities.Strategies
{
	/// <summary>
	/// Converts prices/quantities for ADR (American Depositary Receipt) / GDR (Global Depositary Receipt)
	/// symbols whose <see cref="Model.Symbols.SymbolProfile.SharesPerReceipt"/> is not 1, so that the
	/// adjusted unit price and quantity reflect the underlying ordinary shares consistently, similar to
	/// how <see cref="StockSplitStrategy"/> handles stock splits.
	/// </summary>
	public class ReceiptRatioStrategy : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.ReceiptRatio;

		public Task Execute(Holding holding)
		{
			var symbolsWithRatio = holding.SymbolProfiles.Where(x => x.SharesPerReceipt != 1).ToList();
			if (symbolsWithRatio.Count == 0)
			{
				return Task.CompletedTask;
			}

			var firstRatio = symbolsWithRatio[0].SharesPerReceipt;
			if (symbolsWithRatio.Any(x => x.SharesPerReceipt != firstRatio))
			{
				var differentRatios = string.Join(", ", symbolsWithRatio.Select(x => $"{x.Symbol}={x.SharesPerReceipt}"));
				throw new InvalidOperationException($"Holding contains conflicting SharesPerReceipt values: {differentRatios}");
			}

			var inverseRatio = 1 / firstRatio;

			foreach (var activity in holding.Activities.OfType<ActivityWithQuantityAndUnitPrice>())
			{
				activity.AdjustedUnitPrice = activity.AdjustedUnitPrice.Times(inverseRatio);
				activity.AdjustedQuantity *= firstRatio;
				activity.AdjustedUnitPriceSource.Add(new CalculatedPriceTrace(
					$"Receipt ratio {firstRatio}",
					activity.AdjustedQuantity,
					activity.AdjustedUnitPrice));
			}

			return Task.CompletedTask;
		}
	}
}
