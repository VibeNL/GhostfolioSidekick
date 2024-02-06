
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IActivitiesService
	{
		Task<IEnumerable<Holding>> GetAllActivities();

		Task InsertActivity(SymbolProfile symbolProfile, Activity activity);

		Task UpdateActivity(SymbolProfile symbolProfile, Activity activity);

		Task DeleteActivity(SymbolProfile symbolProfile, Activity activity);

		Task DeleteAll();
	}
}
