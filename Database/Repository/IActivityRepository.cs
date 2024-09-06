using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IActivityRepository
	{
		IEnumerable<Activity> GetAllActivities();

		Task StoreAll(IEnumerable<Activity> activities);
	}
}
