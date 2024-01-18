using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers;

namespace GhostfolioSidekick.FileImporter
{
	internal class HoldingsCollection(IMarketDataManager marketDataManager) : IHoldingsCollection
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
				foreach (var transaction in partialActivityPerAccount.Value.GroupBy(x => x.TransactionId))
				{
					var sourceTransaction = transaction.FirstOrDefault(x => x.SymbolIdentifiers.Length != 0);

					var holding = holdings.Single(x => x.SymbolProfile == null);
					if (sourceTransaction != null)
					{
						holding = await GetorAddHolding(sourceTransaction);
					}

					DetermineActivity(holding, transaction.ToList());
				}
			}
		}

		private void DetermineActivity(Holding holding, List<PartialActivity> transactions)
		{
			var sourceTransaction = transactions.FirstOrDefault(x => x.SymbolIdentifiers.Length != 0) ?? transactions.First();

			var fees = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == ActivityType.Fee).ToList();
			var taxes = transactions.Except([sourceTransaction]).Where(x => x.ActivityType == ActivityType.Tax).ToList();

			// Sanity check
			if (transactions.Count != 1 + fees.Count() + taxes.Count())
			{
				throw new NotSupportedException("Counts of activities do not match!");
			}

			var activity = new Activity(
				sourceTransaction.ActivityType,
				sourceTransaction.Date,
				sourceTransaction.Amount,
				new Money(sourceTransaction.Currency, sourceTransaction.UnitPrice ?? 0))
			{
				Fees = fees.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
				Taxes = taxes.Select(x => new Money(x.Currency, x.Amount * x.UnitPrice ?? 0)),
			};

			holding.Activities.Add(activity);
		}

		private async Task<Holding> GetorAddHolding(PartialActivity activity)
		{
			var symbol = await marketDataManager.FindSymbolByIdentifier(
				activity.SymbolIdentifiers,
				activity.Currency,
				activity.AllowedAssetClasses,
				activity.AllowedAssetSubClasses,
				true,
				false) ?? throw new NotSupportedException();

			var holding = holdings.SingleOrDefault(x => x.SymbolProfile?.Equals(symbol) ?? false);
			if (holding == null)
			{
				holding = new Holding(symbol);
				holdings.Add(holding);
			}

			return holding;
		}
	}
}