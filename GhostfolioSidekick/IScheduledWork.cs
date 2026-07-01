using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public interface IScheduledWork
	{
		TaskPriority Priority { get; }

		TimeSpan ExecutionFrequency { get; }

		bool ExceptionsAreFatal { get; }

		string Name { get; }

		TimeSpan? MaxRunTime { get; }

		Task DoWork(ILogger logger, CancellationToken cancellationToken);
	}
}
