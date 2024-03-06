using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Strategies;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Cryptocurrency
{
	public class ApplyDustCorrectionWorkaround(Settings settings) : IHoldingStrategy
	{
		[ExcludeFromCodeCoverage]
		public int Priority => (int)StrategiesPriority.ApplyDustCorrection;

		public Task Execute(Holding holding)
		{
			if (!settings.CryptoWorkaroundDust || holding.SymbolProfile?.AssetSubClass != AssetSubClass.CryptoCurrency)
			{
				return Task.CompletedTask;
			}

			var activities = holding.Activities.OrderBy(x => x.Date).ToList();

			var amount = activities
				.Select(x => x as IActivityWithQuantityAndUnitPrice)
				.Where(x => x != null)
				.Sum(x => x!.Quantity);

			// Should always be a sell or send as we have dust!
			var lastActivity = activities
				.Select(x => x as IActivityWithQuantityAndUnitPrice)
				.Where(x => x != null)
				.LastOrDefault(x =>
					x!.Quantity < 0);
			if (lastActivity == null || lastActivity.UnitPrice == null)
			{
				return Task.CompletedTask;
			}

			var lastKnownPrice = lastActivity.UnitPrice.Amount;

			decimal dustValue = amount * lastKnownPrice;
			if (dustValue != 0 && Math.Abs(dustValue) < settings.CryptoWorkaroundDustThreshold)
			{
				lastActivity.UnitPrice = new Money(
					lastActivity.UnitPrice.Currency,
					lastActivity.UnitPrice.Amount * ((lastActivity.Quantity - amount) / lastActivity.Quantity));

				lastActivity.Quantity += amount;

				RemoveActivitiesAfter(activities, lastActivity);
				holding.Activities.Clear();
				holding.Activities.AddRange(activities);
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
