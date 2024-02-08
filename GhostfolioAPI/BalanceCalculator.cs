using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public static class BalanceCalculator
	{
		public static async Task<Balance> Calculate(
			Currency baseCurrency,
			IExchangeRateService exchangeRateService,
			IEnumerable<Activity> activities)
		{
			var sortedActivities = activities.OrderByDescending(x => x.Date).ThenBy(x => x.SortingPriority);
			var lastKnownBalance = sortedActivities.FirstOrDefault(x => x.ActivityType == ActivityType.KnownBalance);
			if (lastKnownBalance != null)
			{
				return new Balance(new Money(lastKnownBalance.UnitPrice.Currency, lastKnownBalance.Quantity));
			}

			var amount = 0M;
			foreach (var activity in sortedActivities)
			{
				var factor = 0M;
				switch (activity.ActivityType)
				{
					case ActivityType.CashDeposit:
					case ActivityType.Dividend:
					case ActivityType.Interest:
					case ActivityType.Sell:
						factor = 1;
						break;
					case ActivityType.CashWithdrawal:
					case ActivityType.Buy:
					case ActivityType.Fee:

						factor = -1;
						break;
					case ActivityType.Gift:
					case ActivityType.LearningReward:
					case ActivityType.Receive:
					case ActivityType.Send:
					case ActivityType.StakingReward:
					case ActivityType.Valuable:
					case ActivityType.Liability:
						break;
					case ActivityType.Convert:
						throw new NotSupportedException();
					default:
						throw new NotSupportedException();
				}

				amount += factor * (await exchangeRateService.GetConversionRate(activity.UnitPrice.Currency, baseCurrency, activity.Date)) *
							activity.UnitPrice.Amount * activity.Quantity;
			}

			return new Balance(new Money(baseCurrency, amount));
		}
	}
}
