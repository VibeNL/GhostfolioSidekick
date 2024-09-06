using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class CurrencyGathererTask(ICurrencyRepository currencyRepository, IActivityRepository activityRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CurrencyGatherer;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			var activities = activityRepository.GetAllActivities();

			var currencies = new List<(DateTime, Currency)>();
			foreach (var activity in activities)
			{
				switch (activity)
				{
					case ActivityWithQuantityAndUnitPrice activityWithQuantityAndUnitPrice:
						{
							currencies.Add((activity.Date, activityWithQuantityAndUnitPrice.UnitPrice?.Currency ?? null!));
						}
						break;
					default:
						break;
				}
			}
		}
	}
}
