using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class TimedHostedService : IHostedService
	{
		private CancellationTokenSource? cancellationTokenSource;

		private Task? task;
		private readonly ILogger logger;
		private readonly PriorityQueue<Scheduled, DateTime> workQueue = new();

		public TimedHostedService(ILogger<TimedHostedService> logger, IEnumerable<IScheduledWork> workItems)
		{
			this.logger = logger;

			foreach (var todo in workItems.OrderBy(x => x.Priority))
			{
				workQueue.Enqueue(new Scheduled(todo, DateTime.MinValue), DateTime.MinValue.AddMinutes((int)todo.Priority));
			}
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			cancellationTokenSource = new CancellationTokenSource();

			logger.LogInformation("Service is starting.");

			// create a task that runs continuously
			task = Task.Run(async () =>
			{
				try
				{
					await ExecuteWorkLoop();
				}
				catch (Exception ex)
				{
					logger.LogCritical(ex, "An error occurred executing {Name}. Exception message {Message}", nameof(TimedHostedService), ex.Message);
				}
			}, cancellationTokenSource.Token);

			return Task.CompletedTask;
		}

		private async Task ExecuteWorkLoop()
		{
			while (!cancellationTokenSource!.Token.IsCancellationRequested)
			{
				var workItem = workQueue.Peek();

				if (await ShouldDelayExecution(workItem))
				{
					continue;
				}

				logger.LogInformation("Service {Name} is executing.", workItem.Work.GetType().Name);

				workItem = workQueue.Dequeue();

				await ExecuteWorkItem(workItem);

				HandleWorkItemCompletion(workItem);

				logger.LogInformation("Service {Name} has executed.", workItem.Work.GetType().Name);
			}
		}

		private async Task<bool> ShouldDelayExecution(Scheduled workItem)
		{
			var delay = (workItem.NextSchedule - DateTime.Now).TotalMilliseconds;
			if (delay > 0)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationTokenSource!.Token);
				return true;
			}
			return false;
		}

		private async Task ExecuteWorkItem(Scheduled workItem)
		{
			try
			{
				await workItem.Work.DoWork();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred executing {Name}. Exception message {Message}", workItem.Work.GetType().Name, ex.Message);

				if (workItem.Work.ExceptionsAreFatal)
				{
					throw;
				}
			}
		}

		private void HandleWorkItemCompletion(Scheduled workItem)
		{
			if (workItem.DetermineNextSchedule())
			{
				workQueue.Enqueue(workItem, workItem.NextSchedule);
			}
			else
			{
				logger.LogInformation("Service {Name} is no longer scheduled.", workItem.Work.GetType().Name);
			}
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogDebug("Service is stopping.");

			if (task == null)
			{
				return;
			}

			try
			{
				await cancellationTokenSource!.CancelAsync();
				await task.WaitAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				// ignore
			}

			task.Dispose();
			task = null;
			cancellationTokenSource?.Dispose();
			cancellationTokenSource = null;
		}

		private sealed class Scheduled(IScheduledWork item, DateTime nextSchedule)
		{
			public IScheduledWork Work { get; } = item;

			public DateTime NextSchedule { get; set; } = nextSchedule;

			internal bool DetermineNextSchedule()
			{
				if (Work.ExecutionFrequency == TimeSpan.MaxValue)
				{
					return false;
				}

				NextSchedule = DateTime.Now.Add(Work.ExecutionFrequency);
				return true;
			}
		}
	}
}
