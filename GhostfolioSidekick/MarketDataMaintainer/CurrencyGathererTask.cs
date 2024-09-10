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
			foreach (var activity in activities)
			{
				switch (activity)
				{
					case ActivityWithQuantityAndUnitPrice activityWithQuantityAndUnitPrice:
						{
							Currency key = activityWithQuantityAndUnitPrice.UnitPrice?.Currency ?? null!;
							if (!currencies.ContainsKey(key) || currencies[key] < activity.Date)
							{
								currencies.Remove(key, out _);
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
