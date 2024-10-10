using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IActivityRepository
	{
		Task<IEnumerable<Activity>> GetAllActivities();

		Task StoreAll(IEnumerable<Activity> activities);
	}
}
