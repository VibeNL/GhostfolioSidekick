using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Strategies
{
	public class RoundStrategy : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.Rounding;

		public Task Execute(Holding holding)
		{
			ArgumentNullException.ThrowIfNull(holding, nameof(holding));

			holding
				.Activities
				.OfType<IActivityWithQuantityAndUnitPrice>()
				.ToList()
				.ForEach(x =>
				{
					// Database uses double precision, 8 bytes, variable-precision, inexact, 15 decimal digits precision
					x.Quantity = Math.Round(x.Quantity, 6); // to avoid rounding errors, we round to 6 decimal places

					if (x.UnitPrice != null)
					{
						x.UnitPrice = new Money(x.UnitPrice!.Currency, Math.Round(x.UnitPrice!.Amount, 10));
					}
				});

			return Task.CompletedTask;
		}
	}
}
