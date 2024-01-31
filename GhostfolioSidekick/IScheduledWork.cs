namespace GhostfolioSidekick
{
	public interface IScheduledWork
	{
		TaskPriority Priority { get; }

		Task DoWork();
	}
}
