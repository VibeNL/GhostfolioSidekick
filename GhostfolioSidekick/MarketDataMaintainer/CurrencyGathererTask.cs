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
			var activities = await activityRepository.GetAllActivities();

			var currencies = new Dictionary<Currency, DateTime>();
			foreach (var activity in activities.OrderBy(x => x.Date))
			{
				switch (activity)
				{
					case ActivityWithQuantityAndUnitPrice activityWithQuantityAndUnitPrice:
						{
							Currency? key = activityWithQuantityAndUnitPrice.UnitPrice?.Currency;
							if (key != null && !currencies.ContainsKey(key))
							{
								currencies.Add(key, activity.Date);
							}
						}
						break;
					default:
						break;
				}
			}

			foreach (var item in currencies)
			{
				var currencyHistory = await currencyRepository.GetCurrencyHistory(item.Key, Currency.USD, DateOnly.FromDateTime(item.Value));
				
			}
		}
	}
}
