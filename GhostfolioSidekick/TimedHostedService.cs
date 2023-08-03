using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class TimedHostedService : IHostedService, IDisposable
	{
		private readonly ILogger _logger;
		private readonly IEnumerable<IScheduledWork> _workItems;
		private Timer? _timer;

		public TimedHostedService(ILogger<TimedHostedService> logger, IEnumerable<IScheduledWork> todo)
		{
			_logger = logger;
			_workItems = todo;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Service is starting.");

			// TODO: Make configurable
			_timer = new Timer(DoWork, null, TimeSpan.Zero,
				TimeSpan.FromHours(1));

			return Task.CompletedTask;
		}

		private void DoWork(object? state)
		{
			_logger.LogInformation("Service is executing.");

			foreach (var workItem in _workItems)
			{
				try
				{
					workItem.DoWork().Wait();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex.Message);
				}
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Service is stopping.");

			_timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}
	}
}
