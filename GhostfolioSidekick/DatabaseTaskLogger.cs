using GhostfolioSidekick.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	public class DatabaseTaskLogger(DbContext dbContext, IScheduledWork work, ILogger innerLogger) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return innerLogger.BeginScope(state);
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return innerLogger.IsEnabled(logLevel);
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			// Pass-through to inner logger
			innerLogger.Log(logLevel, eventId, state, exception, formatter);

			// Store log in database
			var message = formatter(state, exception);

			// Find or create TaskRun for this work
			var taskRun = dbContext.Set<TaskRun>()
				.Where(tr => tr.Type == work.GetType().Name || tr.Name == work.Name)
				.AsEnumerable()
				.OrderByDescending(tr => tr.LastUpdate)
				.FirstOrDefault() ?? throw new NotSupportedException($"No TaskRun found for work of type {work.GetType().Name} and name {work.Name}. Ensure that a TaskRun is created before logging.");
			var logEntry = new TaskRunLog
			{
				TaskRunType = work.GetType().Name,
				Timestamp = DateTimeOffset.UtcNow,
				Message = message,
				TaskRun = taskRun
			};

			dbContext.Set<TaskRunLog>().Add(logEntry);
			dbContext.SaveChanges();
		}

		internal void EmptyPreviousLogs()
		{
			var taskRun = dbContext.Set<TaskRun>()
				.Where(tr => tr.Type == work.GetType().Name || tr.Name == work.Name)
				.AsEnumerable()
				.OrderByDescending(tr => tr.LastUpdate)
				.FirstOrDefault();

			if (taskRun != null)
			{
				taskRun.Logs.Clear();
				dbContext.SaveChanges();
			}
		}
	}
}
