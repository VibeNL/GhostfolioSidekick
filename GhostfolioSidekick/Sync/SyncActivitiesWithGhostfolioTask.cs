using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Sync
{
	internal class SyncActivitiesWithGhostfolioTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncActivitiesWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			
			// Only sync activities for accounts that have SyncActivities enabled
			var allActivities = await databaseContext.Activities
				.Include(x => x.Holding)
				.Include(x => x.Account)
				.ToListAsync();
				
			await ghostfolioSync.SyncAllActivities(allActivities);
		}
	}
}
