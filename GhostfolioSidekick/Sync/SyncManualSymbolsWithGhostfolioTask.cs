using Castle.Core.Logging;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Sync
{
	internal class SyncManualSymbolsWithGhostfolioTask(
			IDbContextFactory<DatabaseContext> databaseContextFactory,
			IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncManualActivitiesWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var manualSymbolProfiles = await databaseContext.SymbolProfiles.Where(x => x.DataSource == Datasource.MANUAL).ToListAsync();
			await ghostfolioSync.SyncSymbolProfiles(manualSymbolProfiles);
		}
	}
}
