
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IActivitiesService
	{
		Task<IEnumerable<Holding>> GetAllActivities();

		Task InsertActivity(SymbolProfile symbolProfile, IActivity activity);

		Task UpdateActivity(SymbolProfile symbolProfile, IActivity oldActivity, IActivity newActivity);

		Task DeleteActivity(SymbolProfile symbolProfile, IActivity activity);

		Task DeleteAll();
	}
}
