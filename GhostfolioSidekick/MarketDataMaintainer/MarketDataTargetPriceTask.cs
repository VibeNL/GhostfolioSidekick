using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MarketDataTargetPriceTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		ITargetPriceRepository targetPriceRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataTargetPrice;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => false;

		public string Name => "Getting Target Price Task";

		public async Task DoWork(ILogger logger)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var symbols = await databaseContext.SymbolProfiles
				.Include(s => s.PriceTarget)
				.ToListAsync();

			foreach (var symbol in symbols)
			{
				logger.LogDebug("Processing price targets for symbol {Symbol}", symbol.Symbol);

				try
				{
					var gatheredPriceTarget = await targetPriceRepository.GetPriceTarget(symbol);

					// Replace existing price target or add new one
					symbol.PriceTarget = gatheredPriceTarget;
				} 
				catch (Exception ex)
				{
					logger.LogError(ex, "Error gathering price target for symbol {Symbol}", symbol.Symbol);
				}
			}

			// Save changes
			await databaseContext.SaveChangesAsync();
		}
	}
}
