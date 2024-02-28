﻿using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Parsers;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.FileImporter
{
	internal class HoldingsCollection : IHoldingsCollection
	{
		private readonly List<Holding> holdings = [new Holding(null)];
		private readonly Dictionary<string, List<PartialActivity>> unusedPartialActivities = [];
		private readonly ILogger logger;

		public HoldingsCollection(
			ILogger logger,
			IAccountService accountService,
			IMarketDataService marketDataService)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			AccountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
			MarketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
		}

		public IReadOnlyList<Holding> Holdings { get { return holdings; } }

		public IAccountService AccountService { get; }
		public IMarketDataService MarketDataService { get; }

		public Task AddPartialActivity(string accountName, IEnumerable<PartialActivity> partialActivities)
		{
			if (unusedPartialActivities.TryAdd(accountName, partialActivities.ToList()))
			{
				return Task.CompletedTask;
			}

			unusedPartialActivities[accountName].AddRange(partialActivities);
			return Task.CompletedTask;
		}

		public async Task GenerateActivities()
		{
			var accounts = await AccountService.GetAllAccounts();
			foreach (var partialActivityPerAccount in unusedPartialActivities)
			{
				var account = accounts.Single(x => string.Equals(x.Name, partialActivityPerAccount.Key, StringComparison.InvariantCultureIgnoreCase));
				foreach (var transaction in partialActivityPerAccount.Value.GroupBy(x => x.TransactionId))
				{
					try
					{
						await DetermineActivity(account, [.. transaction]);
					}
					catch (SymbolNotFoundException symbol)
					{
						logger.LogError($"Symbol [{string.Join(",", symbol.SymbolIdentifiers.Select(x => x.Identifier))}] not found for transaction {transaction.Key}. Skipping transaction");
					}
				}
			}

			unusedPartialActivities.Clear();
		}

		public async Task<Dictionary<Account, Balance>> GetAccountBalances(IExchangeRateService exchangeRateService)
		{
			if (unusedPartialActivities.Count != 0)
			{
				throw new NotSupportedException();
			}

			var list = new Dictionary<Account, Balance>();
			foreach (var account in holdings
									.SelectMany(x => x.Activities)
									.Select(x => x.Account)
									.Distinct())
			{
				var balance = await GetBalance(exchangeRateService, account);
				list.Add(account, balance);
			}

			return list;
		}

		private async Task DetermineActivity(Account account, List<PartialActivity> transactions)
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
				sourceTransaction.Description,
				sourceTransaction.SplitFrom,
				sourceTransaction.SplitTo);

			(await GetorAddHolding(sourceTransaction)).Activities.Add(activity);

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
					sourceTransaction.Description,
					sourceTransaction.SplitFrom,
					sourceTransaction.SplitTo);

				(await GetorAddHolding(transaction)).Activities.Add(activity);
			}
		}

		private IActivity GenerateActivity(
			Account account,
			PartialActivityType activityType,
			DateTime date,
			decimal amount,
			Money money,
			string? transactionId,
			IEnumerable<Money> fees,
			IEnumerable<Money> taxes,
			int? sortingPriority,
			string? description,
			int stockSplitFrom,
			int stockSplitTo)
		{
			switch (activityType)
			{
				case PartialActivityType.Buy:
					return new BuySellActivity(account, date, amount, money, transactionId)
					{
						Taxes = taxes,
						Fees = fees,
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.Sell:
					return new BuySellActivity(account, date, -amount, money, transactionId)
					{
						Taxes = taxes,
						Fees = fees,
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.Dividend:
					return new DividendActivity(account, date, money, transactionId)
					{
						Taxes = taxes,
						Fees = fees,
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.Interest:
					return new InterestActivity(account, date, money, transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.Fee:
					return new FeeActivity(account, date, money, transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.CashDeposit:
					return new CashDepositWithdrawalActivity(account, date, money, transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.CashWithdrawal:
					return new CashDepositWithdrawalActivity(account, date, money.Times(-1), transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.KnownBalance:
					return new KnownBalanceActivity(account, date, money, transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.Valuable:
					return new ValuableActivity(account, date, money, transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.Liability:
					return new LiabilityActivity(account, date, money, transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.Gift:
					return new GiftActivity(account, date, amount, transactionId)
					{
						SortingPriority = sortingPriority,
						Description = description,
					};
				case PartialActivityType.StockSplit:
					return new StockSplitActivity(account, date, stockSplitFrom, stockSplitTo, transactionId)
					{
						SortingPriority = sortingPriority,
					};
				default:
					throw new NotSupportedException();
			}
		}

		private async Task<Holding> GetorAddHolding(PartialActivity activity)
		{
			if (activity.SymbolIdentifiers.Length == 0)
			{
				return holdings.Single(x => x.SymbolProfile == null);
			}

			var allowedAssetClass = EmptyToNull(activity.SymbolIdentifiers.SelectMany(x => x.AllowedAssetClasses ?? []));
			var allowedAssetSubClass = EmptyToNull(activity.SymbolIdentifiers.SelectMany(x => x.AllowedAssetSubClasses ?? []));
			var symbol = await MarketDataService.FindSymbolByIdentifier(
				activity.SymbolIdentifiers.Select(x => x.Identifier).ToArray(),
				activity.Currency,
				allowedAssetClass?.ToArray(),
				allowedAssetSubClass?.ToArray(),
				true,
				false);

			if (symbol == null)
			{
				throw new SymbolNotFoundException(activity.SymbolIdentifiers);
			}

			var holding = holdings.SingleOrDefault(x => x.SymbolProfile?.Equals(symbol) ?? false);
			if (holding == null)
			{
				holding = new Holding(symbol);
				holdings.Add(holding);
			}

			return holding;
		}

		private static List<T>? EmptyToNull<T>(IEnumerable<T> array)
		{
			if (array.All(x => x == null))
			{
				return null;
			}

			return array.Where(x => x != null).ToList();
		}

		private Task<Balance> GetBalance(IExchangeRateService exchangeRateService, Account account)
		{
			using (logger.BeginScope($"Balance for account {account.Name}"))
			{
				var allActivities = holdings.SelectMany(x => x.Activities).Where(x => x.Account == account).ToList();
				return new BalanceCalculator(exchangeRateService, logger).Calculate(account.Balance.Money.Currency, allActivities);
			}
		}
	}
}