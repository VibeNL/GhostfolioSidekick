using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
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
			var currencies = (await databaseContext.Activities
				.OfType<ActivityWithQuantityAndUnitPrice>()
				.Select(x => new { Currency = x.UnitPrice!.Currency, Date =x.Date })
				.Distinct()
				.ToListAsync())
				.Where(x => x.Currency != null)
				.GroupBy(x => x.Currency)
				.Select(x => x.OrderBy(y => y.Date).First());

			var currenciesMatches = currencies.SelectMany((l) => currencies, (l, r) => Tuple.Create(l, r)).Where(x => x.Item1.Currency != x.Item2.Currency);

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
					.FirstOrDefaultAsync();

				// Check if we need to update our data
				if (dates != null &&
						DateOnly.FromDateTime(dates.MinDate) == fromDate &&
						dates.MaxDate == DateTime.Today)
				{
					continue;
				}

				var currencyHistory = await currencyRepository.GetCurrencyHistory(match.Item1.Currency, match.Item2.Currency, fromDate);
				if (currencyHistory != null)
				{
					using var writeDatabaseContext = await databaseContextFactory.CreateDbContextAsync();

					var symbolProfile = await writeDatabaseContext.SymbolProfiles
						.Where(x => x.Symbol == symbolString)
						.FirstOrDefaultAsync() ?? new SymbolProfile(symbolString, symbolString, [], match.Item1.Currency with { }, Datasource.YAHOO, AssetClass.Undefined, null, [], []);
					symbolProfile.MarketData.Clear();

					foreach (var item in currencyHistory)
					{
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
