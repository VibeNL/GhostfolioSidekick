using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class TimedHostedService : IHostedService
	{
		private CancellationTokenSource? cancellationTokenSource;

		private Task? task;
		private readonly DatabaseContext databaseContext;
		private readonly ILogger logger;
		private readonly PriorityQueue<Scheduled, DateTimeOffset> workQueue = new();

		public TimedHostedService(
			DatabaseContext databaseContext,
			ILogger<TimedHostedService> logger,
			IEnumerable<IScheduledWork> workItems)
		{
			this.databaseContext = databaseContext;
			this.logger = logger;

			GenerateDatabase().Wait();

			foreach (var todo in workItems.OrderBy(x => x.Priority))
			{
				workQueue.Enqueue(new Scheduled(todo, DateTimeOffset.MinValue), DateTimeOffset.MinValue.AddMinutes((int)todo.Priority));
				InitializeTasks(todo);
			}
		}

		private async Task GenerateDatabase()
		{
			logger.LogInformation("Generating / Updating database...");

			await databaseContext.Database.MigrateAsync().ConfigureAwait(false);
			await databaseContext.ExecutePragma("PRAGMA synchronous=FULL;");
			await databaseContext.ExecutePragma("PRAGMA fullfsync=ON;");
			await databaseContext.ExecutePragma("PRAGMA journal_mode=DELETE;");

			logger.LogInformation("Do integrity checks and vacuum...");
			await databaseContext.ExecutePragma("PRAGMA integrity_check;");
			await databaseContext.Database.ExecuteSqlRawAsync("VACUUM;");

			logger.LogInformation("Database generated / updated.");
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
					Name = todo.Name,
					LastUpdate = DateTimeOffset.MinValue
				};
				databaseContext.Tasks.Add(existingTask);
			}

			// Set scheduled to true
			existingTask.Scheduled = true;
			existingTask.InProgress = false;
			existingTask.Priority = (int)todo.Priority;
			existingTask.NextSchedule = DateTimeOffset.MinValue.AddMinutes((int)todo.Priority);

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

				// Use memory tracking wrapper
				await ExecuteWorkItemWithMemoryTracking(workItem);

				var rescheduled = HandleWorkItemCompletion(workItem);

				await UpdateTask(workItem.Work.GetType().Name, false, rescheduled ? workItem.NextSchedule : DateTimeOffset.MaxValue);

				logger.LogInformation("Service {Name} has executed.", workItem.Work.GetType().Name);
			}
		}

		private async Task UpdateTask(string type, bool inProgress, DateTimeOffset nextSchedule)
		{
			var dbTask = GetTask(databaseContext, type);
			if (dbTask != null)
			{
				dbTask.LastUpdate = DateTimeOffset.Now;
				dbTask.InProgress = inProgress;
				dbTask.NextSchedule = nextSchedule;
				if (inProgress)
				{
					dbTask.LastException = null;
				}

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
			var taskLogger = new DatabaseTaskLogger(databaseContext, workItem.Work, this.logger);
			taskLogger.EmptyPreviousLogs();

			try
			{
				await workItem.Work.DoWork(taskLogger);
			}
			catch (Exception ex)
			{
				// Log the main exception and all inner exceptions
				var exceptionMessage = GetFullExceptionMessage(ex);
				taskLogger.LogError(ex, "An error occurred executing {Name}. Exception message {Message}", workItem.Work.GetType().Name, exceptionMessage);
				var taskrun = GetTask(databaseContext, workItem.Work.GetType().Name);
				if (taskrun != null)
				{
					taskrun.InProgress = false;
					taskrun.LastException = exceptionMessage;
					await databaseContext.SaveChangesAsync();
				}

				if (workItem.Work.ExceptionsAreFatal)
				{
					throw;
				}
			}
		}

		private static string GetFullExceptionMessage(Exception ex)
		{
			var messages = new List<string>();
			var currentException = ex;

			while (currentException != null)
			{
				messages.Add(currentException.Message);
				currentException = currentException.InnerException;
			}

			return string.Join(" -> ", messages);
		}

		private async Task ExecuteWorkItemWithMemoryTracking(Scheduled workItem)
		{
			// Measure memory before execution
			long memoryBefore = GC.GetTotalMemory(false);
			double memoryBeforeMiB = memoryBefore / 1024d / 1024d;

			await ExecuteWorkItem(workItem);

			// Request garbage collection
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GC.WaitForPendingFinalizers();

			// Measure memory after execution and GC
			long memoryAfter = GC.GetTotalMemory(false);
			double memoryAfterMiB = memoryAfter / 1024d / 1024d;
			double memoryLostMiB = memoryAfterMiB - memoryBeforeMiB;
			logger.LogTrace("Memory after executing {Name}: {MemoryAfterMiB:F2} MiB (Delta: {MemoryLostMiB:F2} MiB)", workItem.Work.GetType().Name, memoryAfterMiB, memoryLostMiB);
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

		private sealed class Scheduled(IScheduledWork item, DateTimeOffset nextSchedule)
		{
			public IScheduledWork Work { get; } = item;

			public DateTimeOffset NextSchedule { get; set; } = nextSchedule;

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
