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

		public string Name => "Upcoming Dividends Task";

		public async Task DoWork(ILogger logger)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var symbols = await databaseContext.SymbolProfiles
				.Take(0) // Disabled for now
				.ToListAsync();

			foreach (var symbol in symbols)
			{
				logger.LogTrace("Processing upcoming dividends for symbol {Symbol}", symbol.Symbol);

				var list = await upcomingDividendRepository.Gather(symbol);

				if (list.Count > 0)
				{
					logger.LogInformation("Found {Count} upcoming dividends for symbol {Symbol}", list.Count, symbol.Symbol);
					foreach (var dividend in list)
					{
						logger.LogInformation("Upcoming dividend for symbol {Symbol}: {Amount} on {Date}", symbol.Symbol, dividend.Amount, dividend.ExDividendDate.ToShortDateString());
					}
				}
				else
				{
					logger.LogTrace("No upcoming dividends found for symbol {Symbol}", symbol.Symbol);
				}

			}
		}
	}
}
