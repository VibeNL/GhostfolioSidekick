//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;

//namespace GhostfolioSidekick.Cryptocurrency
//{
//	public class StakeAsDividendWorkaround(Settings settings) : IHoldingStrategy
//	{
//		public int Priority => (int)StrategiesPriority.StakeRewardWorkaround;

//		public Task Execute(Holding holding)
//		{
//			if (!settings.CryptoWorkaroundStakeReward)
//			{
//				return Task.CompletedTask;
//			}

//			var activities = new List<Activity>(holding.Activities);
//			holding.Activities.Clear();
//			holding.Activities.AddRange(ConvertActivities(activities));
//			return Task.CompletedTask;
//		}

//		private IEnumerable<Activity> ConvertActivities(List<Activity> activities)
//		{
//			foreach (var activity in activities)
//			{
//				if (activity.ActivityType != ActivityType.StakingReward)
//				{
//					yield return activity;
//					continue;
//				}

//				yield return new Activity(
//					activity.Account,
//					ActivityType.Buy,
//					activity.Date,
//					activity.Quantity,
//					activity.UnitPrice!,
//					activity.TransactionId
//					)
//				{
//					Fees = activity.Fees,
//					Taxes = activity.Taxes,
//				};

//				yield return new Activity(
//					activity.Account,
//					ActivityType.Dividend,
//					activity.Date,
//					activity.Quantity,
//					activity.UnitPrice!,
//					activity.TransactionId
//					);
//			}
//		}
//	}
//}
