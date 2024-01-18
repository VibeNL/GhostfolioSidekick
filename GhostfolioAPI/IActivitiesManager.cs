
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IActivitiesManager
	{
		void UpdateActivities(IEnumerable<Holding> activities);
	}
}
