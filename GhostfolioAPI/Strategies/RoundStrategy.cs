using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class RoundStrategy : IHoldingStrategy
	{
		// to avoid rounding errors, we round to 5 decimal places
		private int numberOfDecimals = 5;

		public int Priority => (int)StrategiesPriority.Rounding;

		public Task Execute(Holding holding)
		{
			ArgumentNullException.ThrowIfNull(holding, nameof(holding));

			var missingQuantity = 0m;

			// Round quantities and unit prices
			holding
				.Activities
				.OfType<IActivityWithQuantityAndUnitPrice>()
				.ToList()
				.ForEach(x =>
				{
					decimal rounded = Math.Round(x.Quantity, numberOfDecimals, MidpointRounding.ToZero /* Round buy's down and sell's up */);
					missingQuantity += x.Quantity - rounded;
					x.Quantity = rounded;

					if (x.UnitPrice != null)
					{
						x.UnitPrice = new Money(x.UnitPrice!.Currency, Math.Round(x.UnitPrice!.Amount, numberOfDecimals, MidpointRounding.ToNegativeInfinity /* Always round down*/));
					}
				});

			// Remove activities with 0 quantity
			holding
				.Activities
				.OfType<IActivityWithQuantityAndUnitPrice>()
				.Where(x => x.Quantity == 0)
				.ToList()
				.ForEach(x => holding.Activities.Remove(x));

			// Add missing quantity to the last activity
			if (Math.Round(missingQuantity, numberOfDecimals, MidpointRounding.ToZero /* Round buy's down and sell's up */) != 0)
			{
				var lastActivity = holding.Activities.OfType<IActivityWithQuantityAndUnitPrice>().LastOrDefault();
				if (lastActivity is IActivityWithQuantityAndUnitPrice lastActivityWithQuantityAndUnitPrice)
				{
					lastActivityWithQuantityAndUnitPrice.Quantity += missingQuantity;
				}
			}

			return Task.CompletedTask;
		}
	}
}
