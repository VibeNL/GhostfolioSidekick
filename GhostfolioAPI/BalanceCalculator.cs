﻿using GhostfolioSidekick.Model;
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
				.FirstOrDefault(x => x.ActivityType != PartialActivityType.KnownBalance);
			if (lastKnownBalance != null)
			{
				return new Balance(new Money(lastKnownBalance.Currency, lastKnownBalance.Amount));
			}

			List<Tuple<DateTime, Money>> moneyTrail = [];
			foreach (var activity in activities.OrderBy(x => x.Date).ThenBy(x => x.SortingPriority))
			{
				switch (activity)
				{
					case BuySellActivity buySellActivity:
						moneyTrail.Add(Tuple.Create(activity.Date, new Money(buySellActivity.UnitPrice!.Currency, -1 * buySellActivity.UnitPrice.Amount * buySellActivity.Quantity)));
						break;
					case DividendActivity dividendActivity:
						moneyTrail.Add(Tuple.Create(activity.Date, new Money(dividendActivity.Amount!.Currency, dividendActivity.Amount.Amount)));
						break;
					case InterestActivity interestActivity:
						moneyTrail.Add(Tuple.Create(activity.Date, new Money(interestActivity.Amount!.Currency, interestActivity.Amount.Amount)));
						break;
					case FeeActivity feeActivity:
						moneyTrail.Add(Tuple.Create(activity.Date, new Money(feeActivity.Amount!.Currency, -feeActivity.Amount.Amount)));
						break;
					case CashDepositWithdrawalActivity cashDepositWithdrawalActivity:
						moneyTrail.Add(Tuple.Create(activity.Date, new Money(cashDepositWithdrawalActivity.Amount!.Currency, cashDepositWithdrawalActivity.Amount.Amount)));
						break;
					case StockSplitActivity:
					case StakingRewardActivity:
					case GiftActivity:
					case SendAndReceiveActivity:
						// Nothing to track
						break;
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
