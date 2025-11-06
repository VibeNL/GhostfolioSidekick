using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Sync
{
	internal class SyncManualActivitiesmarketDataWithGhostfolioTask(
			IDbContextFactory<DatabaseContext> databaseContextFactory,
			IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncManualActivitiesmarketDataWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Sync Manual Activities and Market Data with Ghostfolio";

		public async Task DoWork(ILogger logger)
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var manualSymbolProfiles = await databaseContext.SymbolProfiles
				.Include(x => x.MarketData)
				.Where(x => x.DataSource == Datasource.MANUAL).ToListAsync();
			await ghostfolioSync.SyncSymbolProfiles(manualSymbolProfiles);

			foreach (var profile in manualSymbolProfiles)
			{
				var list = profile.MarketData.ToList();
				await ghostfolioSync.SyncMarketData(profile, list);
			}
		}
	}
}
