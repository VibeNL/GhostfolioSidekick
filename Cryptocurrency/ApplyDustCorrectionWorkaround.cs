//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;

//namespace GhostfolioSidekick.Cryptocurrency
//{
//	public class ApplyDustCorrectionWorkaround(Settings settings) : IHoldingStrategy
//	{
//		public int Priority => (int)CryptoStrategiesPriority.ApplyDustCorrection;

//		public Task Execute(Holding holding)
//		{
//			if (!settings.CryptoWorkaroundDust || holding.SymbolProfile?.AssetSubClass != AssetSubClass.CryptoCurrency)
//			{
//				return Task.CompletedTask;
//			}

//			var activities = holding.Activities.OrderBy(x => x.Date).ToList();

//			var amount = GetAmount(activities);

//			// Should always be a sell or send as we have dust!
//			var lastActivity = activities
//				.LastOrDefault(x => x.ActivityType == ActivityType.Sell || x.ActivityType == ActivityType.Send);
//			if (lastActivity == null || lastActivity.UnitPrice == null)
//			{
//				return Task.CompletedTask;
//			}

//			var lastKnownPrice = lastActivity.UnitPrice.Amount;

//			decimal dustValue = amount * lastKnownPrice;
//			if (dustValue != 0 && Math.Abs(dustValue) < settings.CryptoWorkaroundDustThreshold)
//			{
//				lastActivity.UnitPrice = new Money(
//					lastActivity.UnitPrice.Currency,
//					lastActivity.UnitPrice.Amount * (lastActivity.Quantity / (lastActivity.Quantity + amount)));
//				lastActivity.Quantity += amount;

//				RemoveActivitiesAfter(activities, lastActivity);
//				holding.Activities.Clear();
//				holding.Activities.AddRange(activities);
//			}

//			return Task.CompletedTask;
//		}

//		private static void RemoveActivitiesAfter(List<IActivity> activities, IActivity lastActivity)
//		{
//			int index = activities.IndexOf(lastActivity) + 1;
//			activities.RemoveRange(index, activities.Count - index);
//		}

//		private static decimal GetAmount(List<IActivity> activities)
//		{
//			return activities.Sum(x => GetFactor(x) * x.Quantity);
//		}

//		private static decimal GetFactor(Activity x)
//		{
//			switch (x.ActivityType)
//			{
//				case ActivityType.Dividend:
//					return 0;
//				case ActivityType.Gift:
//				case ActivityType.LearningReward:
//				case ActivityType.Buy:
//				case ActivityType.Receive:
//				case ActivityType.StakingReward:
//					return 1;
//				case ActivityType.Sell:
//				case ActivityType.Send:
//					return -1;
//				default:
//					throw new NotSupportedException();
//			}
//		}
//	}
//}
