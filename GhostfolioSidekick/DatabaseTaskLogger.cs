using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick
{
	internal class DatabaseTaskLogger : ILogger
	{
		public DatabaseTaskLogger(DbContext dbContext, IScheduledWork work, ILogger logger)
		{
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			throw new NotImplementedException();
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			throw new NotImplementedException();
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			throw new NotImplementedException();
		}
	}
}