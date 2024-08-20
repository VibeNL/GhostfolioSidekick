using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IActivityRepository
	{
		Task StoreAll(IEnumerable<Activity> activities);
	}
}
