namespace GhostfolioSidekick
{
	public interface IScheduledWork
	{
		TaskPriority Priority { get; }

		TimeSpan ExecutionFrequency { get; }

		bool ExceptionsAreFatal { get; }

		Task DoWork();
	}
}
