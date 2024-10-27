
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IActivitiesService
	{
		Task<IEnumerable<Activity>> GetAllActivities();

		Task InsertActivity(SymbolProfile symbolProfile, Activity activity);

		Task UpdateActivity(SymbolProfile symbolProfile, Activity oldActivity, Activity newActivity);

		Task DeleteActivity(SymbolProfile symbolProfile, Activity activity);

		Task DeleteAll();
	}
}
