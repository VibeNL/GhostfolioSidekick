using System.ComponentModel.DataAnnotations;

namespace GhostfolioSidekick.Model.Tasks
{
	public class TaskRunLog
	{
		public long Id { get; set; }

		public string TaskRunType { get; set; } = string.Empty;

		public virtual TaskRun? TaskRun { get; set; }

		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
		[Required]
		public string Message { get; set; } = string.Empty;
	}
}
