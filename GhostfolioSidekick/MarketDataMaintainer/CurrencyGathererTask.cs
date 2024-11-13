using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class CurrencyGathererTask(IDbContextFactory<DatabaseContext> databaseContextFactory, ICurrencyRepository currencyRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CurrencyGatherer;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var currenciesActivities = (await databaseContext.Activities
				.OfType<ActivityWithQuantityAndUnitPrice>()
				.AsNoTracking()
				.Select(x => new { Currency = x.UnitPrice!.Currency, Date =x.Date })
				.Distinct()
				.ToListAsync())
				.Where(x => x.Currency != null)
				.GroupBy(x => x.Currency)
				.Select(x => x.OrderBy(y => y.Date).First());
			var symbolsActivities = (await databaseContext.SymbolProfiles
				.AsNoTracking()
				.Select(x => new { Currency = x.Currency, Date = DateTime.Today })
				.Distinct()
				.ToListAsync())
				.Where(x => x.Currency != null)
				.GroupBy(x => x.Currency)
				.Select(x => x.OrderBy(y => y.Date).First());
			var currencies = currenciesActivities.Concat(symbolsActivities).Distinct().ToList();

			var currenciesMatches = currencies
				.GroupBy(x => x.Currency)
				.Select(x => new { Currency = x.Key, Date = x.Min(x => x.Date) })
				.SelectMany((l) => currencies, (l, r) => Tuple.Create(l, r))
				.Where(x => x.Item1.Currency != x.Item2.Currency);

			foreach (var match in currenciesMatches)
			{
				if (match.Item1.Currency.IsKnownPair(match.Item2.Currency))
				{
					continue;
				}

				string symbolString = match.Item1.Currency.Symbol + match.Item2.Currency.Symbol;
				DateOnly fromDate = DateOnly.FromDateTime(new DateTime[] { match.Item1.Date, match.Item2.Date }.Min());

				var dates = await databaseContext.SymbolProfiles
					.Where(x => x.Symbol == symbolString)
					.SelectMany(x => x.MarketData)
					.GroupBy(x => 1)
					.Select(g => new
					{
						MinDate = g.Min(x => x.Date),
						MaxDate = g.Max(x => x.Date)
					})
					.OrderBy(x => x.MaxDate)
					.FirstOrDefaultAsync();

				var currencyHistory = await currencyRepository.GetCurrencyHistory(match.Item1.Currency, match.Item2.Currency, fromDate);
				if (currencyHistory != null)
				{
					using var writeDatabaseContext = await databaseContextFactory.CreateDbContextAsync();

					var symbolProfile = await writeDatabaseContext.SymbolProfiles
						.Where(x => x.Symbol == symbolString)
						.FirstOrDefaultAsync() ?? new SymbolProfile(symbolString, symbolString, [], match.Item1.Currency with { }, Datasource.YAHOO, AssetClass.Undefined, null, [], []);
					
					foreach (var item in currencyHistory)
					{
						var existing = symbolProfile.MarketData.SingleOrDefault(x => x.Date == item.Date);

						if (existing != null)
						{
							var compareLogic = new CompareLogic() { Config = new ComparisonConfig { MaxDifferences = int.MaxValue, IgnoreObjectTypes = true, MembersToIgnore = ["Id"] } };
							ComparisonResult result = compareLogic.Compare(existing, item);

							if (result.AreEqual)
							{
								continue;
							}
							
							symbolProfile.MarketData.Remove(existing);
						}

						symbolProfile.MarketData.Add(item);
					}

					if (!await databaseContext.SymbolProfiles.ContainsAsync(symbolProfile).ConfigureAwait(false))
					{
						await databaseContext.SymbolProfiles.AddAsync(symbolProfile);
					}

					await databaseContext.SaveChangesAsync();
				}
			}
		}
	}
}
