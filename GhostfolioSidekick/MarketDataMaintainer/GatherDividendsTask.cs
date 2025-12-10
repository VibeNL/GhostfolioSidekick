using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class GatherDividendsTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IDividendRepository dividendRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.GatherDividends;

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
				logger.LogTrace("Processing dividends for symbol {Symbol}", symbol.Symbol);

				// Gather all dividends
				var gatheredDividends = await dividendRepository.GetDividends(symbol);

				// Build lookups by key (ExDividendDate, PaymentDate, DividendType)
				var existingDividends = symbol.Dividends.ToList();
				var existingLookup = existingDividends.ToDictionary(
					d => (d.ExDividendDate, d.PaymentDate, d.DividendType)
				);
				var gatheredLookup = gatheredDividends.ToDictionary(
					d => (d.ExDividendDate, d.PaymentDate, d.DividendType)
				);

				// Upsert gathered dividends
				foreach (var gathered in gatheredDividends)
				{
					if (existingLookup.TryGetValue((gathered.ExDividendDate, gathered.PaymentDate, gathered.DividendType), out var existing))
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
						.Where(existing => !gatheredLookup.ContainsKey((existing.ExDividendDate, existing.PaymentDate, existing.DividendType)))
				)
				{
					symbol.Dividends.Remove(existing);
					databaseContext.Dividends.Remove(existing);
				}

				logger.LogInformation("Upserted {Count} dividends for symbol {Symbol}", gatheredDividends.Count, symbol.Symbol);
			}

			// Save changes
			await databaseContext.SaveChangesAsync();
		}
	}
}
