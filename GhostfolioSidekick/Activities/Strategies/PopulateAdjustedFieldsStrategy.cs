using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Activities.Strategies
{
	internal class PopulateAdjustedFieldsStrategy : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.SetInitialValue;

		public Task Execute(Holding holding)
		{
			foreach (var activity in holding.Activities.OfType<ActivityWithQuantityAndUnitPrice>())
			{
				activity.AdjustedUnitPrice = activity.UnitPrice;
				activity.AdjustedQuantity = activity.Quantity;
				activity.AdjustedUnitPriceSource.Add(new CalculatedPriceTrace("Initial value", activity.AdjustedQuantity, activity.AdjustedUnitPrice));
			}

			return Task.CompletedTask;
		}
	}
}
