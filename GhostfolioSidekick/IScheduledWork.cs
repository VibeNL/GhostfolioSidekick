namespace GhostfolioSidekick
{
	public interface IScheduledWork
	{
		/////string Name { get; }

		TaskPriority Priority { get; }

		TimeSpan ExecutionFrequency { get; }

		bool ExceptionsAreFatal { get; }

		Task DoWork();
	}
}
