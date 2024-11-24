using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Sync
{
	internal class SyncWithGhostfolioTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncWithGhostfolio;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var allAccounts = await databaseContext.Accounts.ToListAsync();
			foreach (var account in allAccounts)
			{
				await ghostfolioSync.SyncAccount(account);
			}

			var allActivities = await databaseContext.Activities.ToListAsync();
			await ghostfolioSync.SyncAllActivities(allActivities);
		}
	}
}
