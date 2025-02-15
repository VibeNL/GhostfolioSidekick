using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IGhostfolioSync
	{
		Task SyncAccount(Account account);

		Task SyncAllActivities(IEnumerable<Activity> allActivities);

		Task SyncSymbolProfiles(IEnumerable<SymbolProfile> manualSymbolProfiles);
	}
}