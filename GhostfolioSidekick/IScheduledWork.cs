namespace GhostfolioSidekick
{
	public interface IScheduledWork
	{
		TaskPriority Priority { get; }

		TimeSpan ExecutionFrequency { get; }

		Task DoWork();
	}
}
