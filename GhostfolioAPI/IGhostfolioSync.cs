using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IGhostfolioSync
	{
		Task SyncAccount(Account account);

		Task SyncAllActivities(IEnumerable<Activity> allActivities, System.Threading.CancellationToken cancellationToken = default);

		Task SyncMarketData(SymbolProfile profile, ICollection<MarketData> list);

		Task SyncSymbolProfiles(IEnumerable<SymbolProfile> manualSymbolProfiles);
	}
}