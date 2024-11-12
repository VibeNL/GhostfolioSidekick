using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IGhostfolioSync
	{
		Task SyncAccount(Account account);
		Task SyncAll(IEnumerable<Activity> allActivities);
	}
}