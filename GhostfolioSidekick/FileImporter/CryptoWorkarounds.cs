using GhostfolioSidekick.Model;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.FileImporter
{
	public class CryptoWorkarounds
	{
		public static IEnumerable<Activity> StakeWorkaround(ICollection<Activity> activities)
		{
			foreach (var activity in activities)
			{
				if (activity.ActivityType != ActivityType.StakingReward)
				{
					yield return activity;
					continue;
				}

				activity.ActivityType = ActivityType.Buy;
				activity.Comment += " Stake Reward";
				yield return activity;

				var div = new Activity(
					ActivityType.Dividend,
					activity.Asset,
					activity.Date,
					activity.Quantity,
					activity.UnitPrice,
					[],
					TransactionReferenceUtilities.GetComment(activity.ReferenceCode + "_stake_reward_workaround", activity.UnitPrice.Currency.Symbol),
					activity.ReferenceCode + "_stake_reward_workaround");
				yield return div;
			}
		}

		public static IEnumerable<Activity> DustWorkaround(IEnumerable<Activity> activities, decimal dustThreashold)
		{
			foreach (var holding in CalculateHoldings(activities))
			{
				holding.ApplyDustCorrection(dustThreashold);
				foreach (var activity in holding.Activities)
				{
					yield return activity;
				}
			}
		}

		private static IEnumerable<Holding> CalculateHoldings(IEnumerable<Activity> values)
		{
			var list = new ConcurrentDictionary<SymbolProfile, Holding>();

			foreach (var item in values.Where(x => x.Asset != null))
			{
				var holding = list.GetOrAdd(item.Asset!, new Holding());
				holding.AddActivity(item);
			}

			return list.Values;
		}

		internal class Holding
		{
			public List<Activity> Activities { get; } = [];

			internal void AddActivity(Activity item)
			{
				Activities.Add(item);
			}

			internal void ApplyDustCorrection(decimal dustThreashold)
			{
				var amount = GetAmount();
				// Should always be a sell or send as we have dust!
				var lastActivity = Activities.Where(x => x.ActivityType == ActivityType.Sell || x.ActivityType == ActivityType.Send).OrderBy(x => x.Date).LastOrDefault();
				if (lastActivity == null)
				{
					return;
				}

				var lastKnownPrice = lastActivity.UnitPrice.Amount;
				decimal dustValue = amount * lastKnownPrice;
				if (Math.Abs(dustValue) < dustThreashold && dustValue != 0)
				{
					lastActivity.UnitPrice = lastActivity.UnitPrice.Times(lastActivity.Quantity / (lastActivity.Quantity + amount));
					lastActivity.Quantity += amount;

					RemoveActivitiesAfter(lastActivity);
				}
			}

			private void RemoveActivitiesAfter(Activity lastActivity)
			{
				int index = Activities.IndexOf(lastActivity);
				Activities.RemoveRange(index, Activities.Count - index - 1);
			}

			private decimal GetAmount()
			{
				return Activities.Sum(x => GetFactor(x) * x.Quantity);
			}

			private decimal GetFactor(Activity x)
			{
				switch (x.ActivityType)
				{
					case ActivityType.Dividend:
						return 0;
					case ActivityType.Gift:
					case ActivityType.LearningReward:
					case ActivityType.Buy:
					case ActivityType.Receive:
					case ActivityType.StakingReward:
						return 1;
					case ActivityType.Sell:
					case ActivityType.Send:
						return -1;
					default:
						throw new NotSupportedException();
				}
			}
		}

	}
}
