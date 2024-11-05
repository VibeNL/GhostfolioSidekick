namespace GhostfolioSidekick.Sync
{
	internal class SyncWithGhostfolioTask(/*IActivitiesService activitiesService*/) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncWithGhostfolio;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public Task DoWork()
		{
			//var existing = await activitiesService.GetAllActivities();
			throw new NotImplementedException();
		}
	}
}
