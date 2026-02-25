using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class PredictDividendsTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.PredictDividends;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => false;

		public string Name => "Predict Dividends Task";

		public async Task DoWork(ILogger logger)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();

			var lastKnownDate = await databaseContext.CalculatedSnapshots
				.MaxAsync(x => (DateOnly?)x.Date);

			if (lastKnownDate == null)
			{
				return;
			}

			var today = DateOnly.FromDateTime(DateTime.Today);
			var horizon = today.AddMonths(12);
			var lookbackStart = today.AddYears(-2);

			// Build holdings map: symbol -> (quantity, dataSource)
			var holdingsWithProfiles = await databaseContext.Holdings
				.Include(h => h.SymbolProfiles)
				.ToListAsync();

			var snapshots = await databaseContext.CalculatedSnapshots
				.Where(s => s.Date == lastKnownDate)
				.ToListAsync();

			var holdingsMap = new Dictionary<string, (decimal Quantity, string DataSource)>();
			foreach (var holding in holdingsWithProfiles)
			{
				var quantity = snapshots
					.Where(s => s.HoldingId == holding.Id)
					.Sum(s => s.Quantity);

				var sp = holding.SymbolProfiles.FirstOrDefault();
				if (sp?.Symbol != null && quantity > 0)
				{
					holdingsMap[sp.Symbol] = (quantity, sp.DataSource);
				}
			}

			var heldSymbols = holdingsMap.Keys.ToList();

			// Load all data before making changes
			var existingPredictions = await databaseContext.Dividends
				.Where(d => d.DividendState == DividendState.Predicted)
				.ToListAsync();

			var historicalDividends = await databaseContext.Dividends
				.Where(d => d.PaymentDate < today
						 && d.PaymentDate >= lookbackStart
						 && (d.DividendType == DividendType.Cash || d.DividendType == DividendType.CashInterim)
						 && d.DividendState == DividendState.Paid
						 && d.Amount.Amount > 0
						 && d.SymbolProfileSymbol != null && heldSymbols.Contains(d.SymbolProfileSymbol))
				.OrderBy(d => d.PaymentDate)
				.ToListAsync();

			var confirmedUpcoming = await databaseContext.Dividends
				.Where(d => d.PaymentDate >= today
						 && d.DividendState != DividendState.Predicted
						 && d.SymbolProfileSymbol != null && heldSymbols.Contains(d.SymbolProfileSymbol))
				.ToListAsync();

			// Replace all existing predictions with freshly computed ones
			databaseContext.Dividends.RemoveRange(existingPredictions);

			var bySymbol = historicalDividends
				.GroupBy(d => d.SymbolProfileSymbol!)
				.ToDictionary(g => g.Key, g => g.OrderBy(d => d.PaymentDate).ToList());

			var addedCount = 0;
			foreach (var (symbol, (_, dataSource)) in holdingsMap)
			{
				if (!bySymbol.TryGetValue(symbol, out var history) || history.Count < 2)
					continue;

				var intervals = history
					.Zip(history.Skip(1), (a, b) => b.PaymentDate.DayNumber - a.PaymentDate.DayNumber)
					.Where(i => i > 0)
					.ToList();

				if (intervals.Count == 0) continue;

				var intervalDays = Median(intervals);
				if (intervalDays < 14) continue;

				var recentHistory = history.TakeLast(Math.Min(4, history.Count)).ToList();
				var avgDividendPerShare = recentHistory.Average(d => d.Amount.Amount);
				var nativeCurrency = recentHistory.Last().Amount.Currency;

				var projectedDate = history.Last().PaymentDate.AddDays(intervalDays);
				var tolerance = intervalDays / 3.0;

				while (projectedDate <= horizon)
				{
					if (projectedDate >= today)
					{
						var alreadyCovered = confirmedUpcoming.Any(c =>
							c.SymbolProfileSymbol == symbol &&
							Math.Abs(c.PaymentDate.DayNumber - projectedDate.DayNumber) < tolerance);

						if (!alreadyCovered)
						{
							databaseContext.Dividends.Add(new Dividend
							{
								ExDividendDate = projectedDate.AddDays(-14),
								PaymentDate = projectedDate,
								DividendType = DividendType.Cash,
								DividendState = DividendState.Predicted,
								Amount = new Money(nativeCurrency, avgDividendPerShare),
								SymbolProfileSymbol = symbol,
								SymbolProfileDataSource = dataSource
							});

							addedCount++;
						}
					}

					projectedDate = projectedDate.AddDays(intervalDays);
				}
			}

			await databaseContext.SaveChangesAsync();
			logger.LogInformation("Dividend prediction completed: {Added} predictions for {Symbols} symbols", addedCount, holdingsMap.Count);
		}

		private static int Median(List<int> intervals)
		{
			var sorted = intervals.OrderBy(x => x).ToList();
			return sorted[sorted.Count / 2];
		}
	}
}
