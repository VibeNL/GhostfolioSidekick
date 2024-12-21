using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Sync
{
	internal class SyncActivitiesWithGhostfolioTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncActivitiesWithGhostfolio;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var allActivities = await databaseContext.Activities.Include(x => x.Holding).ToListAsync();
			await ghostfolioSync.SyncAllActivities(allActivities);
		}
	}
}
