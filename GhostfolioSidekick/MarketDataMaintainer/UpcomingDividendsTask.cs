using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class UpcomingDividendsTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IUpcomingDividendRepository upcomingDividendRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.UpcomingDividends;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Upcoming Dividends Task";

		public async Task DoWork(ILogger logger)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var symbols = await databaseContext.SymbolProfiles
				.Where(sp => sp.AssetSubClass == Model.Activities.AssetSubClass.Stock)
				.ToListAsync();

			foreach (var symbol in symbols)
			{
				logger.LogInformation("Processing upcoming dividends for symbol {Symbol}", symbol.Symbol);

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
					logger.LogInformation("No upcoming dividends found for symbol {Symbol}", symbol.Symbol);
				}

			}

		}
	}
}
