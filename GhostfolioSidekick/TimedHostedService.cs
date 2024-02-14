using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class TimedHostedService : IHostedService
	{
		private readonly ILogger logger;
		private readonly IEnumerable<IScheduledWork> workItems;
		private readonly Timer timer;
		private volatile bool isRunning = false;

		public TimedHostedService(ILogger<TimedHostedService> logger, IEnumerable<IScheduledWork> todo)
		{
			this.logger = logger;
			workItems = todo;

			timer = new Timer(DoWork);
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service is starting.");

			timer.Change(TimeSpan.Zero, TimeSpan.FromHours(1));

			return Task.CompletedTask;
		}

		private void DoWork(object? state)
		{
			lock (logger)
			{
				if (isRunning)
				{
					logger.LogWarning("Service is still executing, skipping run.");
					return;
				}

				isRunning = true;
			}

			try
			{
				logger.LogInformation("Service is executing.");

				foreach (var workItem in workItems.OrderBy(x => x.Priority))
				{
					try
					{
						workItem.DoWork().Wait();
					}
					catch (Exception ex)
					{
						logger.LogError(ex.Message);
					}
				}

				logger.LogInformation("Service has executed.");
			}
			finally
			{
				isRunning = false;
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogInformation("Service is stopping.");

			timer.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}
	}
}
