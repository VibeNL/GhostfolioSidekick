using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers;

namespace GhostfolioSidekick.FileImporter
{
	internal class HoldingsCollection(
		IAccountService accountManager,
		IMarketDataService marketDataManager) : IHoldingsCollection
	{
		private readonly List<Holding> holdings = [new Holding(null)];
		private readonly Dictionary<string, List<PartialActivity>> unusedPartialActivities = [];

		public IReadOnlyList<Holding> Holdings { get { return holdings; } }

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
			foreach (var partialActivityPerAccount in unusedPartialActivities)
			{
				var account = await GetAccount(partialActivityPerAccount.Key);
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
				};

				holding.Activities.Add(activity);
			}
		}

		private async Task<Holding> GetorAddHolding(PartialActivity activity)
		{
			var allowedAssetClass = EmptyToNull(activity.SymbolIdentifiers.SelectMany(x => x.AllowedAssetClasses ?? []).ToArray());
			var allowedAssetSubClass = EmptyToNull(activity.SymbolIdentifiers.SelectMany(x => x.AllowedAssetSubClasses ?? []).ToArray());
			var symbol = await marketDataManager.FindSymbolByIdentifier(
				activity.SymbolIdentifiers.Select(x => x.Identifier).ToArray(),
				activity.Currency,
				allowedAssetClass,
				allowedAssetSubClass,
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

		private static T[]? EmptyToNull<T>(T[] array)
		{
			if (array.All(x => x == null))
			{
				return null;
			}

			return array.Where(x => x != null).ToArray();
		}

		private async Task<Account> GetAccount(string key)
		{
			return await accountManager.GetAccountByName(key);
		}

	}
}