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

			// Build symbol -> holdingId map for per-share dividend calculation
			var symbolToHoldingId = new Dictionary<string, int>();
			foreach (var holding in holdingsWithProfiles)
			{
				var sp = holding.SymbolProfiles.FirstOrDefault();
				if (sp?.Symbol != null)
					symbolToHoldingId[sp.Symbol] = holding.Id;
			}

			// Load historical snapshots to determine quantity held at each dividend date
			var heldHoldingIds = symbolToHoldingId.Values.ToList();
			var historicalSnapshots = await databaseContext.CalculatedSnapshots
				.Where(s => heldHoldingIds.Contains(s.HoldingId)
						 && s.Date >= lookbackStart
						 && s.Date < today)
				.ToListAsync();

			var snapshotsByHolding = historicalSnapshots
				.GroupBy(s => s.HoldingId)
				.ToDictionary(
					g => g.Key,
					g => g.GroupBy(s => s.Date)
						   .Select(dg => (Date: dg.Key, Quantity: dg.Sum(s => s.Quantity)))
						   .OrderBy(x => x.Date)
						   .ToList());

			// Load all data before making changes
			var existingPredictions = await databaseContext.Dividends
				.Where(d => d.DividendState == DividendState.Predicted)
				.ToListAsync();

			var historicalDividends = await databaseContext.Activities
				.OfType<GhostfolioSidekick.Model.Activities.Types.DividendActivity>()
				.Where(a => a.Date < today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
						&& a.Date >= lookbackStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
						&& a.Amount.Amount > 0
						&& a.PartialSymbolIdentifiers.Any(p => heldSymbols.Contains(p.Identifier)))
				.OrderBy(a => a.Date)
				.ToListAsync();

			var confirmedUpcoming = await databaseContext.Activities
				.OfType<GhostfolioSidekick.Model.Activities.Types.DividendActivity>()
				.Where(a => a.Date >= today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
					&& a.PartialSymbolIdentifiers.Any(p => heldSymbols.Contains(p.Identifier)))
				.ToListAsync();

			// Also include upcoming dividends from the Dividends table (non-predicted)
			var confirmedUpcomingDivs = await databaseContext.Dividends
				.Where(d => d.PaymentDate >= today
					&& d.DividendState != DividendState.Predicted
					&& d.SymbolProfileSymbol != null && heldSymbols.Contains(d.SymbolProfileSymbol))
				.ToListAsync();

			// Combine both sources for alreadyCovered check
			var allConfirmedUpcoming = confirmedUpcoming.Cast<object>().ToList();
			allConfirmedUpcoming.AddRange(confirmedUpcomingDivs);

			// Replace all existing predictions with freshly computed ones
			databaseContext.Dividends.RemoveRange(existingPredictions);

			var bySymbol = historicalDividends
				.SelectMany(d => d.PartialSymbolIdentifiers.Select(p => new { Symbol = p.Identifier, Dividend = d }))
				.GroupBy(x => x.Symbol)
				.ToDictionary(g => g.Key, g => g.Select(x => x.Dividend).OrderBy(d => d.Date).ToList());

			var addedCount = 0;
			foreach (var (symbol, (_, dataSource)) in holdingsMap)
			{
				if (!bySymbol.TryGetValue(symbol, out var history) || history.Count < 2)
					continue;

				var intervals = history
					.Zip(history.Skip(1), (a, b) => (int)(b.Date - a.Date).TotalDays)
					.Where(i => i > 0)
					.ToList();

				if (intervals.Count == 0) continue;

				var intervalDays = Median(intervals);
				if (intervalDays < 14) continue;

				var recentHistory = history.TakeLast(Math.Min(4, history.Count)).ToList();
				var perShareAmounts = recentHistory
						.Select(d =>
						{
							if (!symbolToHoldingId.TryGetValue(symbol, out var holdingId))
							{
								return (decimal?)null;
							}

							if (!snapshotsByHolding.TryGetValue(holdingId, out var snapList))
							{
								return null;
							}

							var divDate = DateOnly.FromDateTime(d.Date);
							var snap = snapList.LastOrDefault(s => s.Date <= divDate);
							if (snap.Quantity <= 0)
							{
								snap = snapList.FirstOrDefault(); // fall back to earliest available snapshot
							}

							if (snap.Quantity <= 0)
							{
								return null;
							}

							return d.Amount.Amount / snap.Quantity;
						})
					.Where(v => v.HasValue)
					.Select(v => v!.Value)
					.ToList();

				if (perShareAmounts.Count == 0) continue;
				var avgDividendPerShare = perShareAmounts.Average();
				var nativeCurrency = recentHistory.Last().Amount.Currency;

				var projectedDate = DateOnly.FromDateTime(history.Last().Date).AddDays(intervalDays);
				var tolerance = intervalDays / 3.0;

				while (projectedDate <= horizon)
				{
					if (projectedDate >= today)
					{
						var alreadyCovered = allConfirmedUpcoming.Any(c =>
						{
							if (c is GhostfolioSidekick.Model.Activities.Types.DividendActivity act)
							{
								return act.PartialSymbolIdentifiers.Any(p => p.Identifier == symbol) &&
									Math.Abs(DateOnly.FromDateTime(act.Date).DayNumber - projectedDate.DayNumber) < tolerance;
							}
							else if (c is Dividend div)
							{
								return div.SymbolProfileSymbol == symbol &&
									Math.Abs(div.PaymentDate.DayNumber - projectedDate.DayNumber) < tolerance;
							}
							return false;
						});

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
