namespace GhostfolioSidekick.Model.Tasks
{
	public class TaskRun
	{
		public DateTimeOffset LastUpdate { get; set; }

		public string Type { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;

		public bool Scheduled { get; set; }

		public int Priority { get; set; }

		public DateTimeOffset? NextSchedule { get; set; }
		public bool InProgress { get; set; }
		
		
	}
}
