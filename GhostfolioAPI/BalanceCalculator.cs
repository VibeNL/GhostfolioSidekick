using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class BalanceCalculator
	{
		private readonly IExchangeRateService exchangeRateService;
		private readonly ILogger logger;

		public BalanceCalculator(
			IExchangeRateService exchangeRateService,
			ILogger logger)
		{
			this.exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task<Balance> Calculate(
			Currency baseCurrency,
			IEnumerable<Activity> activities)
		{
			var descendingSortedActivities = activities.OrderByDescending(x => x.Date).ThenBy(x => x.SortingPriority);
			var lastKnownBalance = descendingSortedActivities.FirstOrDefault(x => x.ActivityType == ActivityType.KnownBalance);
			if (lastKnownBalance != null)
			{
				logger.LogDebug($"Known balance {lastKnownBalance.Quantity} {lastKnownBalance.UnitPrice.Currency.Symbol}");
				return new Balance(new Money(lastKnownBalance.UnitPrice.Currency, lastKnownBalance.Quantity));
			}

			var totalAmount = 0M;
			foreach (var activity in activities.OrderBy(x => x.Date).ThenBy(x => x.SortingPriority))
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

				var activityAmount = factor * (await exchangeRateService.GetConversionRate(activity.UnitPrice.Currency, baseCurrency, activity.Date)) *
							activity.UnitPrice.Amount * activity.Quantity;
				totalAmount += activityAmount;
				logger.LogDebug($"Activity {activity.ActivityType} {factor} {activityAmount}. Total is now: {totalAmount}");
				
			}

			return new Balance(new Money(baseCurrency, totalAmount));
		}
	}
}
