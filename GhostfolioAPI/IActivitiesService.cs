
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IActivitiesService
	{
		Task UpdateActivities(List<string> accountNames, IEnumerable<Holding> holdings);
	}
}
