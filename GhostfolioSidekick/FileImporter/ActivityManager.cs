using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.FileImporter
{
	internal class ActivityManager(
		ILogger logger,
		IAccountRepository accountRepository) : IActivityManager
	{
		private readonly Dictionary<string, List<PartialActivity>> unusedPartialActivities = [];
		private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

		public void AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities)
		{
			if (!unusedPartialActivities.TryAdd(accountName, partialActivities.ToList()))
			{
				unusedPartialActivities[accountName].AddRange(partialActivities);
			}
		}

		public async Task<IEnumerable<Activity>> GenerateActivities()
		{
			var activities = new List<Activity>();
			foreach (var partialActivityPerAccount in unusedPartialActivities)
			{
				var accountName = partialActivityPerAccount.Key;
				var account = await accountRepository.GetAccountByName(accountName) ?? new Account(accountName);
				foreach (var transaction in partialActivityPerAccount.Value.GroupBy(x => x.TransactionId))
				{
					DetermineActivity(activities, account, [.. transaction]);
				}
			}

			unusedPartialActivities.Clear();

			return activities;
		}

		private void DetermineActivity(List<Activity> activities, Account account, List<PartialActivity> transactions)
		{
			var sourceTransaction = transactions.Find(x => x.SymbolIdentifiers.Length != 0) ?? transactions[0];

			var fees = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == PartialActivityType.Fee).ToList();
			var taxes = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == PartialActivityType.Tax).ToList();

			var otherTransactions = transactions.Except([sourceTransaction]).Except(fees).Except(taxes);

			var activity = GenerateActivity(
				account,
				sourceTransaction.ActivityType,
				sourceTransaction.Date,
				sourceTransaction.Amount,
				new Money(sourceTransaction.Currency, sourceTransaction.UnitPrice ?? 0),
				sourceTransaction.TransactionId,
				fees.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
				taxes.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
				sourceTransaction.SortingPriority,
				sourceTransaction.Description ?? "<EMPTY>");

			activities.Add(activity);

			int counter = 2;
			foreach (var transaction in otherTransactions)
			{
				activity = GenerateActivity(
					account,
					transaction.ActivityType,
					transaction.Date,
					transaction.Amount,
					new Money(transaction.Currency, transaction.UnitPrice ?? 0),
					sourceTransaction.TransactionId + $"_{counter++}",
					[],
					[],
					transaction.SortingPriority,
					sourceTransaction.Description ?? "<EMPTY>");

				activities.Add(activity);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Required for generating")]
		private static Activity GenerateActivity(
			Account account,
			PartialActivityType activityType,
			DateTime date,
			decimal amount,
			Money money,
			string? transactionId,
			IEnumerable<Money> fees,
			IEnumerable<Money> taxes,
			int? sortingPriority,
			string? description)
		{
			switch (activityType)
			{
				case PartialActivityType.Buy:
					return new BuySellActivity(account, date, amount, money, transactionId, sortingPriority, description)
					{
						Taxes = taxes.ToList(),
						Fees = fees.ToList(),
					};
				case PartialActivityType.Sell:
					return new BuySellActivity(account, date, -amount, money, transactionId, sortingPriority, description)
					{
						Taxes = taxes.ToList(),
						Fees = fees.ToList(),
					};
				case PartialActivityType.Receive:
					return new SendAndReceiveActivity(account, date, amount, transactionId, sortingPriority, description)
					{
						Fees = fees.ToList(),
					};
				case PartialActivityType.Send:
					return new SendAndReceiveActivity(account, date, -amount, transactionId, sortingPriority, description)
					{
						Fees = fees.ToList(),
					};
				case PartialActivityType.Dividend:
					return new DividendActivity(account, date, money.Times(amount), transactionId, sortingPriority, description)
					{
						Taxes = taxes.ToList(),
						Fees = fees.ToList(),
					};
				case PartialActivityType.Interest:
					return new InterestActivity(account, date, money.Times(amount), transactionId, sortingPriority, description);
				case PartialActivityType.Fee:
					return new FeeActivity(account, date, money.Times(amount), transactionId, sortingPriority, description);
				case PartialActivityType.CashDeposit:
					return new CashDepositWithdrawalActivity(account, date, money.Times(amount), transactionId, sortingPriority, description);
				case PartialActivityType.CashWithdrawal:
					return new CashDepositWithdrawalActivity(account, date, money.Times(-amount), transactionId, sortingPriority, description);
				case PartialActivityType.KnownBalance:
					return new KnownBalanceActivity(account, date, money, transactionId, sortingPriority, description);
				case PartialActivityType.Valuable:
					return new ValuableActivity(account, date, money.Times(amount), transactionId, sortingPriority, description);
				case PartialActivityType.Liability:
					return new LiabilityActivity(account, date, money.Times(amount), transactionId, sortingPriority, description);
				case PartialActivityType.Gift:
					return new GiftActivity(account, date, amount, transactionId, sortingPriority, description);
				case PartialActivityType.StakingReward:
					return new StakingRewardActivity(account, date, amount, transactionId, sortingPriority, description);
				case PartialActivityType.BondRepay:
					return new RepayBondActivity(account, date, money.Times(amount), transactionId, sortingPriority, description);
				default:
					throw new NotSupportedException($"GenerateActivity PartialActivityType.{activityType} not yet implemented");
			}
		}
	}
}