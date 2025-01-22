using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class CurrencyGathererTask(IDbContextFactory<DatabaseContext> databaseContextFactory, ICurrencyRepository currencyRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CurrencyGatherer;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		public async Task DoWork()
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var currenciesActivities = (await databaseContext.Activities
				.OfType<ActivityWithQuantityAndUnitPrice>()
				.AsNoTracking()
				.Select(x => new { x.UnitPrice!.Currency, x.Date })
				.Distinct()
				.ToListAsync()).Union(
					(await databaseContext.Activities
					.OfType<CashDepositWithdrawalActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync())
				).Union(
					(await databaseContext.Activities
					.OfType<DividendActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync())
				).Union(
					(await databaseContext.Activities
					.OfType<FeeActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync())
				).Union(
					(await databaseContext.Activities
					.OfType<InterestActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync())
				).Union(
					(await databaseContext.Activities
					.OfType<KnownBalanceActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync())
				).Union(
					(await databaseContext.Activities
					.OfType<RepayBondActivity>()
					.AsNoTracking()
					.Select(x => new { x.TotalRepayAmount!.Currency, x.Date })
					.Distinct()
					.ToListAsync())
				) // TODO Refactor and include all activity types and fees and taxes
				.Where(x => x.Currency != null)
				.GroupBy(x => x.Currency)
				.Select(x => x.OrderBy(y => y.Date).First());
			var symbolsActivities = (await databaseContext.SymbolProfiles
				.AsNoTracking()
				.Select(x => new { x.Currency, Date = DateTime.Today })
				.Distinct()
				.ToListAsync())
				.Where(x => x.Currency != null)
				.Select(x => new { Currency = x.Currency!.GetSourceCurrency().Item1, x.Date })
				.GroupBy(x => x.Currency)
				.Select(x => x.OrderBy(y => y.Date).First());
			var currencies = currenciesActivities.Concat(symbolsActivities).Distinct().ToList();

			var currenciesMinDate = currencies
				.GroupBy(x => x.Currency.Symbol)
				.Select(x => new { x.First().Currency, Date = x.Min(x => x.Date) });

			var currenciesMatches = currenciesMinDate
				.Join(currenciesMinDate, x => 1, x => 1, (x, y) => new { Item1 = x, Item2 = y })
				.Where(x => x.Item1.Currency != x.Item2.Currency)
				.ToList();

			foreach (var match in currenciesMatches)
			{
				string symbolString = match.Item1.Currency.Symbol + match.Item2.Currency.Symbol;
				DateOnly fromDate = DateOnly.FromDateTime(new DateTime[] { match.Item1.Date, match.Item2.Date }.Min());

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

					if (!await writeDatabaseContext.SymbolProfiles.ContainsAsync(symbolProfile).ConfigureAwait(false))
					{
						await writeDatabaseContext.SymbolProfiles.AddAsync(symbolProfile);
					}

					await writeDatabaseContext.SaveChangesAsync();
				}
			}
		}
	}
}
