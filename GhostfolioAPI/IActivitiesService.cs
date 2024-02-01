
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IActivitiesService
	{
		Task<IEnumerable<Holding>> GetAllActivities();

		Task UpdateActivities(List<string> accountNames, IEnumerable<Holding> holdings);

		Task DeleteAll();
	}
}
