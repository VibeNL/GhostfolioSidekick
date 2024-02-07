﻿using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Parsers;

namespace GhostfolioSidekick.FileImporter
{
	internal class HoldingsCollection : IHoldingsCollection
	{
		private readonly List<Holding> holdings = [new Holding(null)];
		private readonly Dictionary<string, List<PartialActivity>> unusedPartialActivities = [];

		public HoldingsCollection(
			IAccountService accountService,
			IMarketDataService marketDataService)
		{
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
					var sourceTransaction = transaction.FirstOrDefault(x => x.SymbolIdentifiers.Length != 0);

					var holding = holdings.Single(x => x.SymbolProfile == null);
					if (sourceTransaction != null)
					{
						holding = await GetorAddHolding(sourceTransaction);
					}

					DetermineActivity(account, holding, [.. transaction]);
				}
			}

			unusedPartialActivities.Clear();
		}

		private void DetermineActivity(Account account, Holding holding, List<PartialActivity> transactions)
		{
			var sourceTransaction = transactions.Find(x => x.SymbolIdentifiers.Length != 0) ?? transactions[0];

			var fees = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == ActivityType.Fee).ToList();
			var taxes = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == ActivityType.Tax).ToList();

			var otherTransactions = transactions.Except([sourceTransaction]).Except(fees).Except(taxes);

			var activity = new Activity(
				account,
				sourceTransaction.ActivityType,
				sourceTransaction.Date,
				sourceTransaction.Amount,
				new Money(sourceTransaction.Currency, sourceTransaction.UnitPrice ?? 0),
				sourceTransaction.TransactionId)
			{
				Fees = fees.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
				Taxes = taxes.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
				SortingPriority = sourceTransaction.SortingPriority,
				Description = sourceTransaction.Description,
			};
			holding.Activities.Add(activity);

			int counter = 2;
			foreach (var transaction in otherTransactions)
			{
				activity = new Activity(
					account,
					transaction.ActivityType,
					transaction.Date,
					transaction.Amount,
					new Money(transaction.Currency, transaction.UnitPrice ?? 0),
					sourceTransaction.TransactionId + $"_{counter++}")
				{
					SortingPriority = transaction.SortingPriority,
					Description = sourceTransaction.Description,
				};

				holding.Activities.Add(activity);
			}
		}

		private async Task<Holding> GetorAddHolding(PartialActivity activity)
		{
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
				throw new NotSupportedException();
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

		public async Task<IEnumerable<Account>> UpdateAccountBalances(IExchangeRateService exchangeRateService)
		{
			if (unusedPartialActivities.Any())
			{
				throw new NotSupportedException();
			}

			var accounts = new List<Account>();
			foreach (var account in holdings
									.SelectMany(x => x.Activities)
									.Select(x => x.Account)
									.Distinct())
			{
				account.Balance = await GetBalance(exchangeRateService, account);
				accounts.Add(account);
			}

			return accounts;
		}

		private Task<Balance> GetBalance(IExchangeRateService exchangeRateService, Account account)
		{
			var allActivities = holdings.SelectMany(x => x.Activities).Where(x => x.Account == account).ToList();
			return BalanceCalculator.Calculate(account.Balance.Money.Currency, exchangeRateService, allActivities);
		}
	}
}