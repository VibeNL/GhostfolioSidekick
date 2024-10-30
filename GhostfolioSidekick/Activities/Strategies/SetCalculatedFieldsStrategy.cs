using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Activities.Strategies
{
	internal class SetCalculatedFieldsStrategy : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.SetInitialValue;

		public Task Execute(Holding holding)
		{
			foreach (var activity in holding.Activities.OfType<ActivityWithQuantityAndUnitPrice>())
			{
				activity.CalculatedUnitPrice = activity.UnitPrice;
				activity.CalculatedQuantity = activity.Quantity;
				activity.CalculatedUnitPriceSource.Add(new CalculatedPriceTrace("Initial value", activity.CalculatedQuantity, activity.CalculatedUnitPrice));
			}

			return Task.CompletedTask;
		}
	}
}
