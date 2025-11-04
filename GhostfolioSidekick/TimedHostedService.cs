using GhostfolioSidekick.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GhostfolioSidekick
{
	public class TimedHostedService : IHostedService
	{
		private CancellationTokenSource? cancellationTokenSource;

		private Task? task;
		private readonly DatabaseContext databaseContext;
		private readonly ILogger logger;
		private readonly PriorityQueue<Scheduled, DateTime> workQueue = new();

		public TimedHostedService(
			DatabaseContext databaseContext,
			ILogger<TimedHostedService> logger,
			IEnumerable<IScheduledWork> workItems)
		{
			this.databaseContext = databaseContext;
			this.logger = logger;

			foreach (var todo in workItems.OrderBy(x => x.Priority))
			{
				workQueue.Enqueue(new Scheduled(todo, DateTime.MinValue), DateTime.MinValue.AddMinutes((int)todo.Priority));
				InitializeTasks(todo);
			}
		}

		private void InitializeTasks(IScheduledWork todo)
		{
			// Check if exists
			Model.Tasks.TaskRun? existingTask = GetTask(databaseContext, todo.GetType().Name);
			if (existingTask == null)
			{
				existingTask = new Model.Tasks.TaskRun
				{
					Type = todo.GetType().Name!,
					Name = todo.GetType().Name!, // TODO: Change to a more friendly name if needed
					LastUpdate = DateTimeOffset.MinValue
				};
				databaseContext.Tasks.Add(existingTask);
			}

			// Set scheduled to true
			existingTask.Scheduled = true;
			existingTask.InProgress = false;
			existingTask.Priority = (int)todo.Priority;
			existingTask.NextSchedule = DateTime.MinValue.AddMinutes((int)todo.Priority);

			databaseContext.SaveChanges();
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

				await UpdateTask(workItem.Work.GetType().Name, true, workItem.NextSchedule);

				workItem = workQueue.Dequeue();

				await ExecuteWorkItem(workItem);

				var rescheduled = HandleWorkItemCompletion(workItem);

				await UpdateTask(workItem.Work.GetType().Name, false, workItem.NextSchedule);

				logger.LogInformation("Service {Name} has executed.", workItem.Work.GetType().Name);
			}
		}

		private async Task UpdateTask(string type, bool inProgress, DateTime nextSchedule)
		{
			var dbTask = GetTask(databaseContext, type);
			if (dbTask != null)
			{
				dbTask.LastUpdate = DateTimeOffset.Now;
				dbTask.InProgress = inProgress;
				dbTask.NextSchedule = nextSchedule;
				dbTask.LastException = null;
				await databaseContext.SaveChangesAsync();
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
				var taskrun = GetTask(databaseContext, workItem.Work.GetType().Name);
				if (taskrun != null)
				{
					taskrun.InProgress = false;
					taskrun.LastException = ex.Message;
					await databaseContext.SaveChangesAsync();
				}

				if (workItem.Work.ExceptionsAreFatal)
				{
					throw;
				}
			}
		}

		private bool HandleWorkItemCompletion(Scheduled workItem)
		{
			if (workItem.DetermineNextSchedule())
			{
				workQueue.Enqueue(workItem, workItem.NextSchedule);
				return true;
			}

			logger.LogInformation("Service {Name} is no longer scheduled.", workItem.Work.GetType().Name);
			return false;
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

		private static Model.Tasks.TaskRun? GetTask(DatabaseContext databaseContext, string type)
		{
			return databaseContext.Tasks.SingleOrDefault(x => x.Type == type);
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
