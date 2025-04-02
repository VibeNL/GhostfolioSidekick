using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ProcessingService.Sync
{
	internal class SyncAccountsWithGhostfolioTask(IDbContextFactory<DatabaseContext> databaseContextFactory, IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncAccountsWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var allAccounts = await databaseContext.Accounts.ToListAsync();
			foreach (var account in allAccounts)
			{
				await ghostfolioSync.SyncAccount(account);
			}
		}
	}
}
