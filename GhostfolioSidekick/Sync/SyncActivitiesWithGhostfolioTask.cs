using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Sync
{
	internal class SyncActivitiesWithGhostfolioTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncActivitiesWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var allActivities = await databaseContext.Activities.Include(x => x.Holding).ToListAsync();
			await ghostfolioSync.SyncAllActivities(allActivities);
		}
	}
}
