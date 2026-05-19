using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.AccountMaintainer
{
	public class BalanceCalculator(ICurrencyExchange exchangeRateService)
	{
		public async Task<List<Balance>> Calculate(
			Currency baseCurrency,
			IEnumerable<Activity> activities)
		{
			IOrderedEnumerable<Activity> descendingSortedActivities = activities.OrderByDescending(x => x.Date).ThenBy(x => x.SortingPriority);

			// Check if we have known balances
			IEnumerable<KnownBalanceActivity> knownBalances = descendingSortedActivities
				.OfType<KnownBalanceActivity>();
			if (knownBalances.Any())
			{
				return [.. knownBalances
					.OrderByDescending(x => x.Date)
					.Select(x => new Balance(DateOnly.FromDateTime(x.Date), new Money(x.Amount.Currency, x.Amount.Amount)))
					.DistinctBy(x => x.Date)];
			}

			List<Tuple<DateTime, Money>> moneyTrail = [];
			foreach (Activity? activity in activities.OrderBy(x => x.Date).ThenBy(x => x.SortingPriority))
			{
				// Remove the costs of the activity if it has any
				if (activity is IActivityWithCosts activityWithCosts)
				{
					Money costs = Money.Sum(activityWithCosts.Costs);
					moneyTrail.Add(new Tuple<DateTime, Money>(activity.Date, costs.Times(-1)));
				}

				// write a switch on the type of activity
				switch (activity)
				{
					case BuyActivity buySellActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(buySellActivity.Date, buySellActivity.TransactionAmount.Times(-1)));
						break;
					case SellActivity buySellActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(buySellActivity.Date, buySellActivity.TransactionAmount));
						break;
					case CashDepositActivity cashDepositWithdrawalActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(cashDepositWithdrawalActivity.Date, cashDepositWithdrawalActivity.Amount));
						break;
					case CashWithdrawalActivity cashDepositWithdrawalActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(cashDepositWithdrawalActivity.Date, cashDepositWithdrawalActivity.Amount.Times(-1)));
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
						moneyTrail.Add(new Tuple<DateTime, Money>(repayBondActivity.Date, repayBondActivity.Amount));
						break;
					case GiftFiatActivity giftFiatActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(giftFiatActivity.Date, giftFiatActivity.Amount));
						break;
					case CorrectionActivity correctionActivity:
						moneyTrail.Add(new Tuple<DateTime, Money>(correctionActivity.Date, correctionActivity.Amount.Times(-1)));
						break;
					case GiftAssetActivity:
					case LiabilityActivity:
					case ReceiveActivity:
					case SendActivity:
					case StakingRewardActivity:
					case ValuableActivity:
						// No change
						break;
					default:
						throw new NotImplementedException($"Activity type {activity.GetType().Name} is not implemented.");
				}
			}

			List<Balance> balances = [];
			decimal totalAmount = 0m;
			foreach (IGrouping<DateOnly, Tuple<DateTime, Money>> moneyPerDate in moneyTrail.GroupBy(x => DateOnly.FromDateTime(x.Item1)))
			{
				foreach (Tuple<DateTime, Money>? money in moneyPerDate)
				{
					Money activityAmount = await exchangeRateService.ConvertMoney(money.Item2, baseCurrency, DateOnly.FromDateTime(money.Item1));
					totalAmount += activityAmount.Amount;
				}

				balances.Add(new Balance(moneyPerDate.Key, new Money(baseCurrency with { }, totalAmount)));
			}

			return balances;
		}
	}
}
