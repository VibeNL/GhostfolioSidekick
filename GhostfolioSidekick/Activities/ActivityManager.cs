using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers;

namespace GhostfolioSidekick.Activities
{
	internal class ActivityManager(IList<Account> accounts) : IActivityManager
	{
		private readonly Dictionary<string, List<PartialActivity>> unusedPartialActivities = [];

		public void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities)
		{
			if (!unusedPartialActivities.TryAdd(accountName, [.. partialActivities]))
			{
				unusedPartialActivities[accountName].AddRange(partialActivities);
			}
		}

		public Task<IEnumerable<Activity>> GenerateActivities()
		{
			List<Activity> activities = [];
			foreach (KeyValuePair<string, List<PartialActivity>> partialActivityPerAccount in unusedPartialActivities)
			{
				var accountName = partialActivityPerAccount.Key;
				Account account = accounts.FirstOrDefault(x => x.Name == accountName) ?? new Account(accountName);
				foreach (IGrouping<string, PartialActivity> transaction in partialActivityPerAccount.Value.GroupBy(x => x.TransactionId))
				{
					DetermineActivity(activities, account, [.. transaction]);
				}
			}

			unusedPartialActivities.Clear();
			return Task.FromResult<IEnumerable<Activity>>(activities);
		}

		private static void DetermineActivity(List<Activity> activities, Account account, List<PartialActivity> transactions)
		{
			PartialActivity sourceTransaction = transactions.Find(x => x.SymbolIdentifiers.Count != 0) ?? transactions[0];

			List<PartialActivity> fees = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == PartialActivityType.Fee).ToList();
			List<PartialActivity> taxes = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == PartialActivityType.Tax).ToList();

			IEnumerable<PartialActivity> otherTransactions = transactions.Except([sourceTransaction]).Except(fees).Except(taxes);

			Activity? activity = GenerateActivity(
				account,
				sourceTransaction.ActivityType,
				[.. sourceTransaction.SymbolIdentifiers.Where(x => x != null).OfType<PartialSymbolIdentifier>()],
				sourceTransaction.Date,
				sourceTransaction.Amount,
				sourceTransaction.UnitPrice ?? Money.One(Currency.EUR),
				sourceTransaction.TransactionId,
				fees.Select(x => x.TotalTransactionAmount),
				taxes.Select(x => x.TotalTransactionAmount),
				sourceTransaction.TotalTransactionAmount,
				sourceTransaction.SortingPriority,
				sourceTransaction.Description);

			if (activity != null)
			{
				activities.Add(activity);
			}

			int counter = 2;
			foreach (PartialActivity? transaction in otherTransactions)
			{
				activity = GenerateActivity(
					account,
					transaction.ActivityType,
					[.. sourceTransaction.SymbolIdentifiers.Where(x => x != null).OfType<PartialSymbolIdentifier>()],
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
					Taxes = [.. taxes],
					Fees = [.. fees]
				},
				PartialActivityType.Sell => new SellActivity(account, null, partialSymbolIdentifiers, date, amount, money, transactionId, sortingPriority, description)
				{
					Taxes = [.. taxes],
					Fees = [.. fees]
				},
				PartialActivityType.Receive => new ReceiveActivity(account, null, partialSymbolIdentifiers, date, amount, transactionId, sortingPriority, description)
				{
					Fees = [.. fees],
				},
				PartialActivityType.Send => new SendActivity(account, null, partialSymbolIdentifiers, date, amount, transactionId, sortingPriority, description)
				{
					Fees = [.. fees],
				},
				PartialActivityType.Dividend => new DividendActivity(account, null, partialSymbolIdentifiers, date, totalTransactionAmount, transactionId, sortingPriority, description)
				{
					Taxes = [.. taxes],
					Fees = [.. fees],
				},
				PartialActivityType.Interest => new InterestActivity(account, null, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.Fee => new FeeActivity(account, null, date, totalTransactionAmount, transactionId, sortingPriority, description),
				PartialActivityType.Correction => new CorrectionActivity(account, null, date, totalTransactionAmount, transactionId, sortingPriority, description),
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