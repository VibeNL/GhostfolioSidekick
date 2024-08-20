using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Database.Repository
{
	public class ActivityRepository(DatabaseContext databaseContext) : IActivityRepository
	{
		public async Task StoreAll(IEnumerable<Activity> activities)
		{
			await databaseContext.Activities.ExecuteDeleteAsync();
			await databaseContext.Activities.AddRangeAsync(activities);
			await databaseContext.SaveChangesAsync();
		}
	}
}
