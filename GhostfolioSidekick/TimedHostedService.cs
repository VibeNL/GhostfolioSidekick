using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class TimedHostedService : IHostedService
	{
		private CancellationTokenSource? cancellationTokenSource;

		private Task? task;
		private readonly ILogger logger;
		private readonly PriorityQueue<Scheduled, DateTime> workQueue = new PriorityQueue<Scheduled, DateTime>();

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
								while (!cancellationToken.IsCancellationRequested)
								{
									var workItem = workQueue.Peek();

									if (DateTime.Now < workItem.NextSchedule)
									{
										await Task.Delay(Math.Max(workItem.NextSchedule - DateTime.Now, 0));
										continue;
									}

									logger.LogInformation("Service {Name} is executing.", workItem.Work.GetType().Name);

									workItem = workQueue.Dequeue();

									try
									{
										await workItem.Work.DoWork();
									}
									catch (Exception ex)
									{
										logger.LogError(ex, "An error occurred executing {Name}. Exception message {Message}", workItem.Work.GetType().Name, ex.Message);
									}

									if (workItem.DetermineNextSchedule())
									{
										workQueue.Enqueue(workItem, workItem.NextSchedule);
									}
									else
									{
										logger.LogInformation("Service {Name} is no longer scheduled.", workItem.Work.GetType().Name);
									}

									logger.LogInformation("Service {Name} has executed.", workItem.Work.GetType().Name);
								}
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "An error occurred executing {Name}. Exception message {Message}", nameof(TimedHostedService), ex.Message);
							}
						}, cancellationTokenSource.Token);

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			logger.LogDebug("Service is stopping.");

			if (task == null)
			{
				return Task.CompletedTask;
			}

			cancellationTokenSource!.Cancel();
			cancellationTokenSource.Dispose();

			task = null;

			return Task.CompletedTask;
		}

		private sealed class Scheduled
		{
			public Scheduled(IScheduledWork item, DateTime nextSchedule)
			{
				Work = item;
				NextSchedule = nextSchedule;
			}

			public IScheduledWork Work { get; }

			public DateTime NextSchedule { get; set; }

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
