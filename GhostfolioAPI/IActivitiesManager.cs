
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IActivitiesManager
	{
		Task<IEnumerable<Activity>> GetAllActivities();
	}
}
