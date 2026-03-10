using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using GhostfolioSidekick.Parsers;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Activities
{
	internal class ActivityManager(IList<Account> accounts,
									IDbContextFactory<DatabaseContext> databaseContextFactory) : IActivityManager
	{
		private readonly Dictionary<string, List<PartialActivity>> unusedPartialActivities = [];

		public void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities)
		{
			if (!unusedPartialActivities.TryAdd(accountName, [.. partialActivities]))
			{
				unusedPartialActivities[accountName].AddRange(partialActivities);
			}
		}

		public async Task<IEnumerable<Activity>> GenerateActivities()
		{
			var activities = new List<Activity>();
			using var db = databaseContextFactory.CreateDbContext();
			foreach (var partialActivityPerAccount in unusedPartialActivities)
			{
				var accountName = partialActivityPerAccount.Key;
				var account = accounts.FirstOrDefault(x => x.Name == accountName) ?? new Account(accountName);
				// Correct all partials for this account
				var correctedPartials = new List<PartialActivity>(partialActivityPerAccount.Value.Count);
				foreach (var partial in partialActivityPerAccount.Value)
				{
					if (partial.Amount == 0 && (partial.UnitPrice?.Amount ?? 0) == 0 && partial.TotalTransactionAmount != null && partial.TotalTransactionAmount.Amount != 0 && partial.SymbolIdentifiers.Count > 0)
					{
						var symbolId = partial.SymbolIdentifiers.First().Identifier;
						var symbolProfile = await db.SymbolProfiles
							.Include(sp => sp.MarketData)
							.FirstOrDefaultAsync(sp => sp.Identifiers.Contains(symbolId));
						if (symbolProfile != null)
						{
							var date = DateOnly.FromDateTime(partial.Date);
							var marketData = symbolProfile.MarketData
								.OrderBy(md => md.Date)
								.FirstOrDefault(md => md.Date >= date);
							if (marketData != null && marketData.Close > 0)
							{
								correctedPartials.Add(new PartialActivity(
									partial.ActivityType,
									partial.SymbolIdentifiers,
									partial.Date,
									partial.TotalTransactionAmount.Amount / marketData.Close,
									new Money(marketData.Currency, marketData.Close),
									partial.TransactionId,
									partial.TotalTransactionAmount,
									partial.SortingPriority,
									partial.Description
								));
								continue;
							}
						}
					}
					correctedPartials.Add(partial);
				}

				foreach (var transaction in correctedPartials.GroupBy(x => x.TransactionId))
				{
					await DetermineActivity(activities, account, [.. transaction]);
				}
			}

			unusedPartialActivities.Clear();
			return activities;
		}

		private static async Task DetermineActivity(List<Activity> activities, Account account, List<PartialActivity> transactions)
		{
			var sourceTransaction = transactions.Find(x => x.SymbolIdentifiers.Count != 0) ?? transactions[0];
			var fees = transactions.Except(new[] { sourceTransaction }).Where(x => x.ActivityType == PartialActivityType.Fee).ToList();
			var taxes = transactions.Except(new[] { sourceTransaction }).Where(x => x.ActivityType == PartialActivityType.Tax).ToList();
			var otherTransactions = transactions.Except(new[] { sourceTransaction }).Except(fees).Except(taxes);

			var activity = GenerateActivity(
				account,
				sourceTransaction.ActivityType,
				sourceTransaction.SymbolIdentifiers,
				sourceTransaction.Date,
				sourceTransaction.Amount,
				sourceTransaction.UnitPrice ?? Money.One(Currency.EUR),
				sourceTransaction.TransactionId,
				fees.Select(x => new Money(x.UnitPrice?.Currency ?? Currency.EUR, x.Amount * (x.UnitPrice?.Amount ?? 0))),
				taxes.Select(x => new Money(x.UnitPrice?.Currency ?? Currency.EUR, x.Amount * (x.UnitPrice?.Amount ?? 0))),
				sourceTransaction.TotalTransactionAmount,
				sourceTransaction.SortingPriority,
				sourceTransaction.Description);

			if (activity != null)
			{
				activities.Add(activity);
			}

			int counter = 2;
			foreach (var transaction in otherTransactions)
			{
				activity = GenerateActivity(
					account,
					transaction.ActivityType,
					sourceTransaction.SymbolIdentifiers,
					transaction.Date,
					transaction.Amount,
					transaction.UnitPrice ?? Money.One(Currency.EUR),
					sourceTransaction.TransactionId + $"_{counter++}",
					[],
					[],
					null,
					transaction.SortingPriority,
					sourceTransaction.Description);

				if (activity != null)
				{
					activities.Add(activity);
				}
			}
		}

		private static Activity? GenerateActivity(
			Account account,
			PartialActivityType activityType,
			ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers,
			DateTime date,
			decimal amount,
			Money money,
			string transactionId,
			IEnumerable<Money> fees,
			IEnumerable<Money> taxes,
			Money? totalTransactionAmount,
			int? sortingPriority,
			string? description)
		{
			totalTransactionAmount ??= new Money(money.Currency, amount * money.Amount);
			partialSymbolIdentifiers = partialSymbolIdentifiers.Distinct().ToArray();

			return activityType switch
			{
				PartialActivityType.Buy => new BuyActivity(account, null, partialSymbolIdentifiers, date, amount, money, transactionId, sortingPriority, description)
				{
					Taxes = [.. taxes.Select(x => new BuyActivityTax(x))],
					Fees = [.. fees.Select(x => new BuyActivityFee(x))],
					TotalTransactionAmount = totalTransactionAmount,
				},
				PartialActivityType.Sell => new SellActivity(account, null, partialSymbolIdentifiers, date, amount, money, transactionId, sortingPriority, description)
				{
					Taxes = [.. taxes.Select(x => new SellActivityTax(x))],
					Fees = [.. fees.Select(x => new SellActivityFee(x))],
					TotalTransactionAmount = totalTransactionAmount,
				},
				PartialActivityType.Receive => new ReceiveActivity(account, null, partialSymbolIdentifiers, date, amount, transactionId, sortingPriority, description)
				{
					Fees = [.. fees.Select(x => new ReceiveActivityFee(x))],
				},
				PartialActivityType.Send => new SendActivity(account, null, partialSymbolIdentifiers, date, amount, transactionId, sortingPriority, description)
				{
					Fees = [.. fees.Select(x => new SendActivityFee(x))],
				},
				PartialActivityType.Dividend => new DividendActivity(account, null, partialSymbolIdentifiers, date, totalTransactionAmount, transactionId, sortingPriority, description)
				{
					Taxes = [.. taxes.Select(x => new DividendActivityTax(x))],
					Fees = [.. fees.Select(x => new DividendActivityFee(x))],
				},
				PartialActivityType.Interest => new InterestActivity(account, null, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.Fee => new FeeActivity(account, null, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.CashDeposit => new CashDepositActivity(account, null, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.CashWithdrawal => new CashWithdrawalActivity(account, null, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.KnownBalance => new KnownBalanceActivity(account, null, date, money.Times(amount), transactionId, sortingPriority, description),
				PartialActivityType.Valuable => new ValuableActivity(account, null, partialSymbolIdentifiers, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.Liability => new LiabilityActivity(account, null, partialSymbolIdentifiers, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.GiftFiat => new GiftFiatActivity(account, null, date, money.Times(amount), transactionId, sortingPriority, description),
				PartialActivityType.GiftAsset => new GiftAssetActivity(account, null, partialSymbolIdentifiers, date, amount, transactionId, sortingPriority, description),
				PartialActivityType.StakingReward => new StakingRewardActivity(account, null, partialSymbolIdentifiers, date, amount, transactionId, sortingPriority, description),
				PartialActivityType.BondRepay => new RepayBondActivity(account, null, partialSymbolIdentifiers, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.Ignore => null,
				_ => throw new NotSupportedException($"GenerateActivity PartialActivityType.{activityType} not yet implemented"),
			};
		}
	}
}