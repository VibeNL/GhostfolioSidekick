﻿using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Strategies;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.GhostfolioAPI.Strategies
{
	public class ApplyDustCorrection(Settings settings, IMarketDataService marketDataService, ILogger<DeterminePrice> logger) : ApiStrategyBase, IHoldingStrategy
	{
		public ApplyDustCorrection() : base(marketDataService, logger)
		{

		}

		public int Priority => (int)StrategiesPriority.ApplyDustCorrection;

		public async Task Execute(Holding holding)
		{
			if (holding.SymbolProfile == null)
			{
				return;
			}

			var allActivities = holding.Activities.OrderBy(x => x.Date).ToList();

			var totalDustValueQuantity = allActivities
					.OfType<IActivityWithQuantityAndUnitPrice>()
					.Sum(x => x!.Quantity);

			var threshold = settings.DustThreshold;
			if (holding.SymbolProfile?.AssetSubClass == AssetSubClass.CryptoCurrency)
			{
				threshold = settings.CryptoWorkaroundDustThreshold;
			}

			var unitPrice = await GetUnitPrice(holding.SymbolProfile!, DateTime.Today);

			var totalDustValue = unitPrice.Times(totalDustValueQuantity);

			if (totalDustValue.Amount == 0 || Math.Abs(totalDustValue.Amount) >= threshold)
			{
				// No dust to correct
				return Task.CompletedTask;
			}

			var accounts = allActivities.Select(x => x.Account).Distinct();
			foreach (var account in accounts)
			{
				var activities = allActivities
					.Where(x => x.Account == account)
					.OfType<IActivityWithQuantityAndUnitPrice>()
					.ToList();

				var amount = activities
					.Sum(x => x!.Quantity);

				// Should always be a sell or send as we have dust!
				var lastActivity = activities
					.LastOrDefault(x => x!.Quantity < 0);
				if (lastActivity == null)
				{
					return Task.CompletedTask;
				}

				decimal dustValue = amount;

				if (dustValue == 0)
				{
					continue;
				}

				// Remove activities after the last sell activity
				RemoveActivitiesAfter(holding, activities, lastActivity);

				// Get the new amount	
				amount = activities.Sum(x => x!.Quantity);

				// Update unit price of the last activity if possible
				if (lastActivity.UnitPrice != null)
				{
					lastActivity.UnitPrice = new Money(
										lastActivity.UnitPrice.Currency,
										lastActivity.UnitPrice.Amount * (lastActivity.Quantity / (lastActivity.Quantity - amount)));
				}

				// Update the quantity of the last activity
				lastActivity.Quantity -= amount;
			}

			return Task.CompletedTask;
		}

		private static void RemoveActivitiesAfter(Holding holding, List<IActivityWithQuantityAndUnitPrice> activities, IActivityWithQuantityAndUnitPrice lastActivity)
		{
			int index = activities.IndexOf(lastActivity) + 1;
			activities.RemoveRange(index, activities.Count - index);
			holding.Activities.RemoveAll(x => x.Account == lastActivity.Account && !activities.Contains(x));
		}
	}
}
