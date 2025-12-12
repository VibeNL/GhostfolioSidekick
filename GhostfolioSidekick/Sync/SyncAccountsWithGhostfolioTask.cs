using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Sync
{
	internal class SyncAccountsWithGhostfolioTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncAccountsWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Sync Accounts with Ghostfolio";

		public async Task DoWork(ILogger logger)
		{
			await using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			var allAccounts = await databaseContext.Accounts.ToListAsync();
			foreach (var account in allAccounts)
			{
				await ghostfolioSync.SyncAccount(account);
			}
		}
	}
}
