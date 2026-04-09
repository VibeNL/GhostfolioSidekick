using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Performance
{
	internal class UpcomingDividendsTask(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		GhostfolioSidekick.Database.Repository.ICurrencyExchange currencyExchange,
		GhostfolioSidekick.Configuration.IApplicationSettings applicationSettings
	) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.UpcomingDividendsCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Upcoming Dividends Calculations";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Starting upcoming dividends calculation for holdings...");

			using var dbContext = await dbContextFactory.CreateDbContextAsync();

			var today = DateOnly.FromDateTime(DateTime.Today);
			var oneYearFromNow = today.AddYears(1);
			var primaryCurrency = Currency.GetCurrency(applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency) ?? Currency.EUR;
			var holdings = await dbContext.Holdings.Include(h => h.SymbolProfiles).ToListAsync();

			await dbContext.UpcomingDividendTimelineEntries.ExecuteDeleteAsync();

			int totalHoldings = holdings.Count;
			int processedHoldings = 0;
			logger.LogInformation("Total holdings to process for dividends: {Total}", totalHoldings);

           foreach (var holding in holdings)
			{
				try
				{
					decimal totalExpectedReturn = 0;
					decimal totalExpectedReturnPrimary = 0;
					Currency? currency = null;
					bool hasOfficial = false;
					var timelineEntries = new List<UpcomingDividendTimelineEntry>();
					foreach (var symbolProfile in holding.SymbolProfiles)
					{
						var officialDividends = await dbContext.Dividends
							.Where(d => d.SymbolProfileSymbol == symbolProfile.Symbol
								&& d.PaymentDate >= today
								&& d.PaymentDate <= oneYearFromNow
								&& d.DividendState != DividendState.Paid)
							.ToListAsync();

						foreach (var dividend in officialDividends)
						{
							totalExpectedReturn += dividend.Amount.Amount;
							currency ??= dividend.Amount.Currency;
							var converted = await currencyExchange.ConvertMoney(dividend.Amount, primaryCurrency, dividend.PaymentDate);
							totalExpectedReturnPrimary += converted.Amount;
							hasOfficial = true;
							timelineEntries.Add(new UpcomingDividendTimelineEntry
							{
								Id = Guid.NewGuid(),
								HoldingId = holding.Id,
								ExpectedDate = dividend.PaymentDate,
								Amount = dividend.Amount.Amount,
								Currency = dividend.Amount.Currency,
								AmountPrimaryCurrency = converted.Amount,
								DividendType = dividend.DividendType,
								DividendState = dividend.DividendState
							});
						}
					}

					// If no official dividends in the next year, predict from past DividendActivity
					if (!hasOfficial)
					{
						var pastDividends = holding.Activities
							.OfType<GhostfolioSidekick.Model.Activities.Types.DividendActivity>()
							.Where(a => a.Date < DateTime.Today)
							.OrderByDescending(a => a.Date)
							.Take(3)
							.ToList();

						if (pastDividends.Count > 0)
						{
							var avgAmount = pastDividends.Average(a => a.Amount.Amount);
							currency = pastDividends.First().Amount.Currency;
							var intervals = pastDividends.Zip(pastDividends.Skip(1), (a, b) => (a.Date - b.Date).Days).ToList();
							int avgInterval = intervals.Count > 0 ? (int)intervals.Average() : 90;
							int numPeriods = avgInterval > 0 ? (int)Math.Floor(365.0 / avgInterval) : 4;
							var lastDate = pastDividends.First().Date;
							for (int i = 1; i <= numPeriods; i++)
							{
								var expectedDate = DateOnly.FromDateTime(lastDate.AddDays(i * avgInterval));
								totalExpectedReturn += avgAmount;
								var converted = await currencyExchange.ConvertMoney(new Money(currency, avgAmount), primaryCurrency, expectedDate);
								totalExpectedReturnPrimary += converted.Amount;
								timelineEntries.Add(new UpcomingDividendTimelineEntry
								{
									Id = Guid.NewGuid(),
									HoldingId = holding.Id,
									ExpectedDate = expectedDate,
									Amount = avgAmount,
									Currency = currency,
									AmountPrimaryCurrency = converted.Amount,
									DividendType = DividendType.Cash, // Prediction always cash
									DividendState = DividendState.Predicted
								});
							}
						}
					}

                    if (totalExpectedReturn > 0 && currency is not null)
					{
						dbContext.UpcomingDividendTimelineEntries.AddRange(timelineEntries);
						logger.LogInformation("Persisted upcoming dividends timeline for holding {HoldingId}: {Amount} {Currency} ({AmountPrimary} {PrimaryCurrency})", holding.Id, totalExpectedReturn, currency.Symbol, totalExpectedReturnPrimary, primaryCurrency.Symbol);
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error calculating upcoming dividends for holding {HoldingId}", holding.Id);
				}

				processedHoldings++;
				logger.LogInformation("Processed {Processed}/{Total} holdings for dividends", processedHoldings, totalHoldings);
			}

			await dbContext.SaveChangesAsync();
			logger.LogInformation("Upcoming dividends calculation completed for {Count} holdings", totalHoldings);
		}
	}
}
