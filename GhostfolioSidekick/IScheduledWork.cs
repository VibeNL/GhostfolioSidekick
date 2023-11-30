namespace GhostfolioSidekick
{
	public interface IScheduledWork
	{
		int Priority { get; }

		Task DoWork();
	}
}
