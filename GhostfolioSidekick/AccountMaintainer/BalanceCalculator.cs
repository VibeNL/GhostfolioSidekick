using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.AccountMaintainer
{
	public class BalanceCalculator
	{
		private readonly ICurrencyExchange exchangeRateService;

		public BalanceCalculator(ICurrencyExchange exchangeRateService)
		{
			this.exchangeRateService = exchangeRateService;
		}

		public async Task<List<Balance>> Calculate(
			Currency baseCurrency,
			IEnumerable<Activity> activities)
		{
			var descendingSortedActivities = activities.OrderByDescending(x => x.Date).ThenBy(x => x.SortingPriority);

			// Check if we have known balances
			var knownBalances = descendingSortedActivities
				.OfType<KnownBalanceActivity>();
			if (knownBalances.Any())
			{
				return [.. knownBalances
					.OrderByDescending(x => x.Date)
					.Select(x => new Balance(DateOnly.FromDateTime(x.Date), new Money(x.Amount.Currency, x.Amount.Amount)))
					.DistinctBy(x => x.Date)];
			}

			List<Tuple<DateTime, Money>> moneyTrail = [];
			foreach (var activity in activities.OrderBy(x => x.Date).ThenBy(x => x.SortingPriority))
			{
				// write a switch on the type of activity
				switch (activity)
				{
					case BuySellActivity buySellActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(buySellActivity.Date, buySellActivity.TotalTransactionAmount.Times(-1 * Math.Sign(buySellActivity.Quantity))));
						break;
					case CashDepositWithdrawalActivity cashDepositWithdrawalActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(cashDepositWithdrawalActivity.Date, cashDepositWithdrawalActivity.Amount));
						break;
					case DividendActivity dividendActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(dividendActivity.Date, dividendActivity.Amount));
						break;
					case FeeActivity feeActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(feeActivity.Date, feeActivity.Amount.Times(-1)));
						break;
					case InterestActivity interestActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(interestActivity.Date, interestActivity.Amount));
						break;
					case RepayBondActivity repayBondActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(repayBondActivity.Date, repayBondActivity.TotalRepayAmount));
						break;
					case GiftFiatActivity giftFiatActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(giftFiatActivity.Date, giftFiatActivity.Amount));
						break;
					case GiftAssetActivity giftActivity:
					case LiabilityActivity liabilityActivity:
					case SendAndReceiveActivity sendAndReceiveActivity:
					case StakingRewardActivity:
					case ValuableActivity:
						// No change
						break;
					default:
						throw new NotImplementedException($"Activity type {activity.GetType().Name} is not implemented.");
				}
			}

			var balances = new List<Balance>();
			decimal totalAmount = 0m;
			foreach (var moneyPerDate in moneyTrail.GroupBy(x => DateOnly.FromDateTime(x.Item1)))
			{
				foreach (var money in moneyPerDate)
				{
					var activityAmount = await exchangeRateService.ConvertMoney(money.Item2, baseCurrency, DateOnly.FromDateTime(money.Item1));
					totalAmount += activityAmount.Amount;
				}

				balances.Add(new Balance(moneyPerDate.Key, new Money(baseCurrency with { }, totalAmount)));
			}

			return balances;
		}
	}
}
