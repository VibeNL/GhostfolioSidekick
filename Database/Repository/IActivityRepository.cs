using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IActivityRepository
	{
		Task<IEnumerable<Activity>> GetAllActivities();
		Task<bool> HasMatch(Activity activity);
		Task SetMatch(Activity activity, SymbolProfile symbolProfile);
		Task StoreAll(IEnumerable<Activity> activities);
	}
}
