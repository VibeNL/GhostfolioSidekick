using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Performance
{
	internal class UpcomingDividendsActivitiesTask(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		GhostfolioSidekick.Database.Repository.ICurrencyExchange currencyExchange,
		GhostfolioSidekick.Configuration.IApplicationSettings applicationSettings
	) : IScheduledWork
	{
		private sealed record DividendPattern(string Name, int IntervalDays, int ToleranceDays);

		private static readonly DividendPattern MonthlyPattern = new("monthly", 30, 10);
		private static readonly DividendPattern QuarterlyPattern = new("quarterly", 91, 20);
		private static readonly DividendPattern YearlyPattern = new("yearly", 365, 45);

		public TaskPriority Priority => TaskPriority.UpcomingDividendsCalculations;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public bool ExceptionsAreFatal => false;

		public string Name => "Upcoming Dividends Activities Calculations";

		public async Task DoWork(ILogger logger)
		{
			logger.LogInformation("Starting upcoming dividends activities calculation for holdings...");

			using var dbContext = await dbContextFactory.CreateDbContextAsync();

			var today = DateOnly.FromDateTime(DateTime.Today);
			var oneYearFromNow = today.AddYears(1);
			var primaryCurrency = Currency.GetCurrency(applicationSettings.ConfigurationInstance.Settings.PrimaryCurrency) ?? Currency.EUR;
			var holdings = await dbContext.Holdings
				.Include(h => h.SymbolProfiles)
					.ThenInclude(sp => sp.Dividends)
				.Include(h => h.Activities.Where(a => a is DividendActivity))
					.ThenInclude(a => a.Account)
				.Include(h => h.CalculatedSnapshots)
				.AsSplitQuery()
				.ToListAsync();

			await dbContext.Activities
				.OfType<DividendActivity>()
				.Where(a => a.IsPredicted)
				.ExecuteDeleteAsync();

			var createdPredictedActivities = 0;
			var totalHoldings = holdings.Count;
			logger.LogInformation("Total holdings to process for dividends activities: {Total}", totalHoldings);

			foreach (var holding in holdings)
			{
				var historicalDividends = holding.Activities
					.OfType<DividendActivity>()
					.Where(a => !a.IsPredicted && DateOnly.FromDateTime(a.Date) < today)
					.OrderBy(a => a.Date)
					.ToList();

				if (!TryDetectPattern(historicalDividends, out var detectedPattern))
				{
					continue;
				}

				var lastHistoricalDividend = historicalDividends.Last();
				var account = lastHistoricalDividend.Account;
				if (account == null)
				{
					continue;
				}

				var snapshots = holding.CalculatedSnapshots
					.OrderBy(s => s.Date)
					.ToList();

				var predictedDividendPerShare = await CalculatePredictedDividendPerShare(historicalDividends, snapshots, primaryCurrency);
				if (predictedDividendPerShare <= 0)
				{
					continue;
				}

				var defaultSymbolProfile = holding.SymbolProfiles.FirstOrDefault();
				var partialSymbolIdentifiers = BuildPartialSymbolIdentifiers(holding, lastHistoricalDividend, defaultSymbolProfile);

				var occupiedDates = holding.Activities
					.OfType<DividendActivity>()
					.Where(a => !a.IsPredicted && DateOnly.FromDateTime(a.Date) >= today)
					.Select(a => DateOnly.FromDateTime(a.Date))
					.ToList();

				var announcedDividends = holding.SymbolProfiles
					.SelectMany(x => x.Dividends)
					.Where(d => d.PaymentDate >= today && d.PaymentDate <= oneYearFromNow)
					.OrderBy(d => d.PaymentDate)
					.ToList();

				foreach (var announcedDividend in announcedDividends)
				{
					if (IsDateCovered(announcedDividend.PaymentDate, occupiedDates, detectedPattern.ToleranceDays))
					{
						continue;
					}

					var predictedAmount = CalculateProjectedAmount(predictedDividendPerShare, snapshots, announcedDividend.PaymentDate, primaryCurrency);
					if (predictedAmount.Amount <= 0)
					{
						continue;
					}

					dbContext.Activities.Add(new DividendActivity
					{
						Account = account,
						Holding = holding,
						Date = announcedDividend.PaymentDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
						Amount = predictedAmount,
						TransactionId = $"predicted-announced-{holding.Id}-{announcedDividend.PaymentDate:yyyyMMdd}",
						PartialSymbolIdentifiers = [.. partialSymbolIdentifiers],
						Description = $"Predicted announced dividend ({detectedPattern.Name} pattern)",
						IsPredicted = true
					});

					occupiedDates.Add(announcedDividend.PaymentDate);
					createdPredictedActivities++;
				}

				var projectedDate = DateOnly.FromDateTime(lastHistoricalDividend.Date).AddDays(detectedPattern.IntervalDays);
				while (projectedDate <= oneYearFromNow)
				{
					if (projectedDate >= today && !IsDateCovered(projectedDate, occupiedDates, detectedPattern.ToleranceDays))
					{
						var predictedAmount = CalculateProjectedAmount(predictedDividendPerShare, snapshots, projectedDate, primaryCurrency);
						if (predictedAmount.Amount > 0)
						{
							dbContext.Activities.Add(new DividendActivity
							{
								Account = account,
								Holding = holding,
								Date = projectedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
								Amount = predictedAmount,
								TransactionId = $"predicted-unannounced-{holding.Id}-{projectedDate:yyyyMMdd}",
								PartialSymbolIdentifiers = [.. partialSymbolIdentifiers],
								Description = $"Predicted dividend ({detectedPattern.Name} pattern)",
								IsPredicted = true
							});

							occupiedDates.Add(projectedDate);
							createdPredictedActivities++;
						}
					}

					projectedDate = projectedDate.AddDays(detectedPattern.IntervalDays);
				}
			}

			await dbContext.SaveChangesAsync();
			logger.LogInformation(
				"Upcoming dividends activities calculation completed for {Count} holdings with {PredictedCount} predicted activities",
				totalHoldings,
				createdPredictedActivities);
		}

		private static bool TryDetectPattern(List<DividendActivity> historicalDividends, out DividendPattern pattern)
		{
			pattern = MonthlyPattern;
			if (historicalDividends.Count < 3)
			{
				return false;
			}

			var intervals = historicalDividends
				.Zip(historicalDividends.Skip(1), (a, b) => (b.Date - a.Date).TotalDays)
				.Where(days => days > 0)
				.Select(days => (int)Math.Round(days))
				.ToList();

			if (intervals.Count < 2)
			{
				return false;
			}

			var patterns = new[] { MonthlyPattern, QuarterlyPattern, YearlyPattern };
			var best = patterns
				.Select(candidate => new
				{
					Pattern = candidate,
					Matches = intervals.Count(interval => Math.Abs(interval - candidate.IntervalDays) <= candidate.ToleranceDays)
				})
				.OrderByDescending(x => x.Matches)
				.ThenBy(x => x.Pattern.IntervalDays)
				.First();

			var requiredMatches = (int)Math.Ceiling(intervals.Count * 0.6m);
			if (best.Matches < requiredMatches || best.Matches < 2)
			{
				return false;
			}

			pattern = best.Pattern;
			return true;
		}

		private async Task<decimal> CalculatePredictedDividendPerShare(
			List<DividendActivity> historicalDividends,
			List<CalculatedSnapshot> snapshots,
			Currency primaryCurrency)
		{
			var recentDividends = historicalDividends.TakeLast(Math.Min(4, historicalDividends.Count)).ToList();
			var perShareAmounts = new List<decimal>(recentDividends.Count);

			foreach (var dividend in recentDividends)
			{
				var converted = await currencyExchange.ConvertMoney(
					dividend.Amount,
					primaryCurrency,
					DateOnly.FromDateTime(dividend.Date));

				var quantityAtDividendDate = GetQuantityAtDate(snapshots, DateOnly.FromDateTime(dividend.Date));
				if (quantityAtDividendDate > Constants.Epsilon)
				{
					perShareAmounts.Add(converted.Amount / quantityAtDividendDate);
				}
			}

			if (perShareAmounts.Count == 0)
			{
				return 0;
			}

			return perShareAmounts.Average();
		}

		private static Money CalculateProjectedAmount(decimal dividendPerShare, List<CalculatedSnapshot> snapshots, DateOnly date, Currency currency)
		{
			var quantityAtDate = GetQuantityAtDate(snapshots, date);
			if (quantityAtDate <= Constants.Epsilon)
			{
				return Money.Zero(currency);
			}

			return new Money(currency, dividendPerShare * quantityAtDate);
		}

		private static decimal GetQuantityAtDate(List<CalculatedSnapshot> snapshots, DateOnly date)
		{
			if (snapshots.Count == 0)
			{
				return 0;
			}

			var latestSnapshotAtOrBeforeDate = snapshots
				.Where(s => s.Date <= date)
				.OrderByDescending(s => s.Date)
				.FirstOrDefault();

			if (latestSnapshotAtOrBeforeDate != null)
			{
				return latestSnapshotAtOrBeforeDate.Quantity;
			}

			return snapshots
				.OrderBy(s => s.Date)
				.First()
				.Quantity;
		}

		private static List<PartialSymbolIdentifier> BuildPartialSymbolIdentifiers(
			Holding holding,
			DividendActivity referenceDividend,
			GhostfolioSidekick.Model.Symbols.SymbolProfile? symbolProfile)
		{
			if (referenceDividend.PartialSymbolIdentifiers.Count > 0)
			{
				return [.. referenceDividend.PartialSymbolIdentifiers];
			}

			if (holding.PartialSymbolIdentifiers.Count > 0)
			{
				return [.. holding.PartialSymbolIdentifiers];
			}

			if (symbolProfile != null)
			{
				var symbolIdentifier = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, symbolProfile.Symbol, symbolProfile.Currency);
				if (symbolIdentifier != null)
				{
					return [symbolIdentifier];
				}
			}

			return [];
		}

		private static bool IsDateCovered(DateOnly date, IEnumerable<DateOnly> dates, int toleranceDays)
		{
			return dates.Any(existing => Math.Abs(existing.DayNumber - date.DayNumber) <= toleranceDays);
		}
	}
}
