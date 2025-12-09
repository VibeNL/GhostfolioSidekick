using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class GatherDividendsTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IDividendRepository upcomingDividendRepository) : IScheduledWork
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

				// Gather all dividends (upcoming and past)
				var list = await upcomingDividendRepository.Gather(symbol);

				// Delete all existing dividends for the symbol
				symbol.Dividends.Clear();

				// Insert all gathered dividends
				if (list.Count > 0)
				{
					logger.LogInformation("Found {Count} dividends for symbol {Symbol}", list.Count, symbol.Symbol);
					foreach (var dividend in list)
					{
						symbol.Dividends.Add(dividend);
					}
				}
			}

			// Save changes
			await databaseContext.SaveChangesAsync();
		}
	}
}
