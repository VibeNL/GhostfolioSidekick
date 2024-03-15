using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class BalanceCalculator
	{
		private readonly IExchangeRateService exchangeRateService;

		public BalanceCalculator(IExchangeRateService exchangeRateService)
		{
			this.exchangeRateService = exchangeRateService;
		}

		public async Task<Balance> Calculate(
			Currency baseCurrency,
			IEnumerable<PartialActivity> activities)
		{
			var descendingSortedActivities = activities.OrderByDescending(x => x.Date).ThenBy(x => x.SortingPriority);
			var lastKnownBalance = descendingSortedActivities
				.FirstOrDefault(x => x.ActivityType == PartialActivityType.KnownBalance);
			if (lastKnownBalance != null)
			{
				return new Balance(new Money(lastKnownBalance.Currency, lastKnownBalance.Amount));
			}

			List<Tuple<DateTime, Money>> moneyTrail = [];
			foreach (var activity in activities.OrderBy(x => x.Date).ThenBy(x => x.SortingPriority))
			{
				switch (activity.ActivityType)
				{
					case PartialActivityType.Buy:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount.Times(-1)));
						break;
					case PartialActivityType.Sell:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount));
						break;
					case PartialActivityType.Dividend:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount));
						break;
					case PartialActivityType.Interest:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount));
						break;
					case PartialActivityType.Fee:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount.Times(-1)));
						break;
					case PartialActivityType.CashDeposit:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount));
						break;
					case PartialActivityType.CashWithdrawal:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount.Times(-1)));
						break;
					case PartialActivityType.Tax:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount.Times(-1)));
						break;
					case PartialActivityType.Valuable:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount.Times(-1)));
						break;
					case PartialActivityType.Liability:
						moneyTrail.Add(Tuple.Create(activity.Date, activity.TotalTransactionAmount.Times(-1)));
						break;
					case PartialActivityType.Send:
					case PartialActivityType.Receive:
					case PartialActivityType.Gift:
					case PartialActivityType.StakingReward:
					case PartialActivityType.CashConvert:
					case PartialActivityType.KnownBalance:
					case PartialActivityType.StockSplit:
						// ignore for now
						break;
					case PartialActivityType.Undefined:
					default:
						throw new NotSupportedException($"Balance failed to generate, {activity.GetType().Name} not supported");
				}
			}

			var totalAmount = 0m;
			foreach (var money in moneyTrail)
			{
				var activityAmount = (await exchangeRateService.GetConversionRate(money.Item2.Currency, baseCurrency, money.Item1)) * money.Item2.Amount;
				totalAmount += activityAmount;
			}

			return new Balance(new Money(baseCurrency, totalAmount));
		}
	}
}
