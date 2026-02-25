using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class GatherDividendsTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IDividendRepository dividendRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataDividends;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Gather Dividends Task";

		public async Task DoWork(ILogger logger)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var symbols = await databaseContext.SymbolProfiles
				.Include(s => s.Dividends)
				.ToListAsync();

			foreach (var symbol in symbols)
			{
				if (!await dividendRepository.IsSymbolSupported(symbol))
				{
					continue;
				}

				logger.LogDebug("Processing dividends for symbol {Symbol}", symbol.Symbol);

				// Gather all dividends
				var gatheredDividends = await dividendRepository.GetDividends(symbol);

				// Build lookups by key (ExDividendDate, PaymentDate, DividendType)
				var existingDividends = symbol.Dividends.ToList();
				var existingLookup = existingDividends.ToDictionary(
					d => (d.ExDividendDate, d.PaymentDate, d.DividendType, d.DividendState)
				);
				var gatheredLookup = gatheredDividends.ToDictionary(
					d => (d.ExDividendDate, d.PaymentDate, d.DividendType, d.DividendState)
				);

				// Upsert gathered dividends
				foreach (var gathered in gatheredDividends)
				{
					if (existingLookup.TryGetValue((gathered.ExDividendDate, gathered.PaymentDate, gathered.DividendType, gathered.DividendState), out var existing))
					{
						// Update properties if changed
						existing.Amount = gathered.Amount;
						existing.DividendState = gathered.DividendState;
					}
					else
					{
						// Add new dividend
						symbol.Dividends.Add(gathered);
					}
				}

				foreach (var existing in existingDividends
						.Where(existing => existing.DividendState != DividendState.Predicted
										&& !gatheredLookup.ContainsKey((existing.ExDividendDate, existing.PaymentDate, existing.DividendType, existing.DividendState)))
				)
				{
					symbol.Dividends.Remove(existing);
					databaseContext.Dividends.Remove(existing);
				}

				logger.LogDebug("Upserted {Count} dividends for symbol {Symbol}", gatheredDividends.Count, symbol.Symbol);
			}

			// Save changes
			await databaseContext.SaveChangesAsync();
		}
	}
}
