using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Strategies
{
	public class RoundStrategy : IHoldingStrategy
	{
		// to avoid rounding errors, we round to 5 decimal places
		private int numberOfDecimals = 5;

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
					x.Quantity = Math.Round(x.Quantity, numberOfDecimals, MidpointRounding.ToZero /* Round buy's down and sell's up */);

					if (x.UnitPrice != null)
					{
						x.UnitPrice = new Money(x.UnitPrice!.Currency, Math.Round(x.UnitPrice!.Amount, numberOfDecimals, MidpointRounding.ToNegativeInfinity /* Always round down*/));
					}
				});

			return Task.CompletedTask;
		}
	}
}
