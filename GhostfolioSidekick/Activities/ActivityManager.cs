using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
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
			var activities = new List<Activity>();
			foreach (var partialActivityPerAccount in unusedPartialActivities)
			{
				var accountName = partialActivityPerAccount.Key;
				var account = accounts.FirstOrDefault(x => x.Name == accountName) ?? new Account(accountName);
				foreach (var transaction in partialActivityPerAccount.Value.GroupBy(x => x.TransactionId))
				{
					DetermineActivity(activities, account, [.. transaction]);
				}
			}

			unusedPartialActivities.Clear();
			return Task.FromResult<IEnumerable<Activity>>(activities);
		}

		private static void DetermineActivity(List<Activity> activities, Account account, List<PartialActivity> transactions)
		{
			var sourceTransaction = transactions.Find(x => x.SymbolIdentifiers.Count != 0) ?? transactions[0];

			var fees = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == PartialActivityType.Fee).ToList();
			var taxes = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == PartialActivityType.Tax).ToList();

			var otherTransactions = transactions.Except([sourceTransaction]).Except(fees).Except(taxes);

			var activity = GenerateActivity(
				account,
				sourceTransaction.ActivityType,
				sourceTransaction.SymbolIdentifiers,
				sourceTransaction.Date,
				sourceTransaction.Amount,
				new Money(sourceTransaction.Currency, sourceTransaction.UnitPrice ?? 0),
				sourceTransaction.TransactionId,
				fees.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
				taxes.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
				sourceTransaction.TotalTransactionAmount,
				sourceTransaction.SortingPriority,
				sourceTransaction.Description);

			activities.Add(activity);

			int counter = 2;
			foreach (var transaction in otherTransactions)
			{
				activity = GenerateActivity(
					account,
					transaction.ActivityType,
					sourceTransaction.SymbolIdentifiers,
					transaction.Date,
					transaction.Amount,
					new Money(transaction.Currency, transaction.UnitPrice ?? 0),
					sourceTransaction.TransactionId + $"_{counter++}",
					[],
					[],
					null,
					transaction.SortingPriority,
					sourceTransaction.Description);

				activities.Add(activity);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Required for generating")]
		private static Activity GenerateActivity(
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
			totalTransactionAmount = totalTransactionAmount ?? new Money(money.Currency, amount * money.Amount);
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
				_ => throw new NotSupportedException($"GenerateActivity PartialActivityType.{activityType} not yet implemented"),
			};
		}
	}
}