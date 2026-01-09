using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class CurrencyGathererTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		ICurrencyRepository currencyRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CurrencyGatherer;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Currency Gatherer";

		public async Task DoWork(ILogger logger)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var currenciesActivities = (await databaseContext.Activities
				.OfType<ActivityWithQuantityAndUnitPrice>()
				.AsNoTracking()
				.Select(x => new { x.UnitPrice!.Currency, x.Date })
				.Distinct()
				.ToListAsync()).Union(
					await databaseContext.Activities
					.OfType<CashDepositActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync()
				).Union(
					await databaseContext.Activities
					.OfType<CashWithdrawalActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync()
				).Union(
					await databaseContext.Activities
					.OfType<DividendActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync()
				).Union(
					await databaseContext.Activities
					.OfType<FeeActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync()
				).Union(
					await databaseContext.Activities
					.OfType<InterestActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync()
				).Union(
					await databaseContext.Activities
					.OfType<KnownBalanceActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync()
				).Union(
					await databaseContext.Activities
					.OfType<RepayBondActivity>()
					.AsNoTracking()
					.Select(x => new { x.Amount!.Currency, x.Date })
					.Distinct()
					.ToListAsync()
				)
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
				var sourceCurrency = match.Item1.Currency;
				var targetCurrency = match.Item2.Currency;
				DateOnly fromDate = DateOnly.FromDateTime(new DateTime[] { match.Item1.Date, match.Item2.Date }.Min());

				var currencyHistory = await currencyRepository.GetCurrencyHistory(match.Item1.Currency, match.Item2.Currency, fromDate);
				if (currencyHistory != null)
				{
					using var writeDatabaseContext = await databaseContextFactory.CreateDbContextAsync();

					var currencyExchangeProfile = await writeDatabaseContext.CurrencyExchangeRates
						.Where(x => x.SourceCurrency == sourceCurrency && x.TargetCurrency == targetCurrency)
						.FirstOrDefaultAsync() ?? new CurrencyExchangeProfile(sourceCurrency, targetCurrency);

					foreach (var item in currencyHistory)
					{
						var existing = currencyExchangeProfile.Rates.SingleOrDefault(x => x.Date == item.Date);

						if (existing != null)
						{
							existing.CopyFrom(item);
						}
						else
						{
							currencyExchangeProfile.Rates.Add(item);
						}
					}

					if (!await writeDatabaseContext.CurrencyExchangeRates.ContainsAsync(currencyExchangeProfile).ConfigureAwait(false))
					{
						await writeDatabaseContext.CurrencyExchangeRates.AddAsync(currencyExchangeProfile);
					}

					await writeDatabaseContext.SaveChangesAsync();
				}
			}
		}
	}
}
