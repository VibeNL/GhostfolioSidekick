namespace GhostfolioSidekick
{
	public interface IScheduledWork
	{
		TaskPriority Priority { get; }

		TimeSpan ExecutionFrequency { get; }

		bool ExceptionsAreFatal { get; }
		
		string Name { get; }

		Task DoWork();
	}
}
