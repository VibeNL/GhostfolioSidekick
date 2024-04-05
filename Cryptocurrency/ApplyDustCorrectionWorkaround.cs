using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Strategies;

namespace GhostfolioSidekick.Cryptocurrency
{
	public class ApplyDustCorrectionWorkaround(Settings settings) : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.ApplyDustCorrection;

		public Task Execute(Holding holding)
		{
			if (!settings.CryptoWorkaroundDust || holding.SymbolProfile?.AssetSubClass != AssetSubClass.CryptoCurrency)
			{
				return Task.CompletedTask;
			}

			var allActivities = holding.Activities.OrderBy(x => x.Date).ToList();

			var accounts = allActivities.Select(x => x.Account).Distinct();
			foreach (var account in accounts)
			{
				var activities = allActivities
					.Where(x => x.Account == account)
					.ToList();

				var amount = activities
					.Select(x => x as IActivityWithQuantityAndUnitPrice)
					.Where(x => x != null)
					.Sum(x => x!.Quantity);

				// Should always be a sell or send as we have dust!
				var lastActivity = activities
					.OfType<IActivityWithQuantityAndUnitPrice>()
					.LastOrDefault(x =>
						x!.Quantity < 0);
				if (lastActivity == null)
				{
					return Task.CompletedTask;
				}

				decimal dustValue = amount;
				if (dustValue != 0 && Math.Abs(dustValue) < settings.CryptoWorkaroundDustThreshold)
				{

if(lastActivity.UnitPrice != null){					lastActivity.UnitPrice = new Money(
						lastActivity.UnitPrice.Currency,
						lastActivity.UnitPrice.Amount * ((lastActivity.Quantity - amount) / lastActivity.Quantity));}

					lastActivity.Quantity -= amount;

					RemoveActivitiesAfter(activities, lastActivity);
					holding.Activities.RemoveAll(x => x.Account == account && !activities.Contains(x));
					//holding.Activities.Clear();
					//holding.Activities.AddRange(activities);
				}
			}

			return Task.CompletedTask;
		}

		private static void RemoveActivitiesAfter(List<IActivity> activities, IActivity lastActivity)
		{
			int index = activities.IndexOf(lastActivity) + 1;
			activities.RemoveRange(index, activities.Count - index);
		}
	}
}
