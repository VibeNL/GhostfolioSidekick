using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IActivityRepository
	{
		Task<Holding?> FindHolding(IList<PartialSymbolIdentifier> ids);

		Task<IEnumerable<Activity>> GetAllActivities();

		Task<IEnumerable<Holding>> GetAllHoldings();

		Task Store(Holding holding);
		
		Task StoreAll(IEnumerable<Activity> activities);
	}
}
