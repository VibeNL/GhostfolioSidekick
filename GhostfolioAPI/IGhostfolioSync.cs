using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IGhostfolioSync
	{
		Task SyncAccount(Account account);
		Task SyncAllActivities(IEnumerable<Activity> allActivities);
	}
}