using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Activities.Strategies
{
	public class CleanTrace : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.CleanTrace;

		public Task Execute(Holding holding)
		{
			var activities = holding.Activities
				.OrderBy(x => x.Date).ToList();

			if (activities.Count == 0)
			{
				return Task.CompletedTask;
			}

			foreach (var activity in activities.OfType<ActivityWithQuantityAndUnitPrice>())
			{
				CleanAdjustedUnitPriceSource(activity);
			}

			return Task.CompletedTask;
		}

		private static void CleanAdjustedUnitPriceSource(ActivityWithQuantityAndUnitPrice activity)
		{
			activity.AdjustedUnitPriceSource.Clear();
		}
	}
}
