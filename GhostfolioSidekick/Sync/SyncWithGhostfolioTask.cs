using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI;

namespace GhostfolioSidekick.Sync
{
	internal class SyncWithGhostfolioTask(IActivityRepository activityRepository, IAccountRepository accountRepository, IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncWithGhostfolio;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public async Task DoWork()
		{
			var allAccounts = await accountRepository.GetAllAccounts();
			foreach (var account in allAccounts)
			{
				await ghostfolioSync.SyncAccount(account);
			}

			var allActivities = await activityRepository.GetAllActivities();
			await ghostfolioSync.SyncAllActivities(allActivities);
		}
	}
}
