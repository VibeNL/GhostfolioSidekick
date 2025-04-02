using GhostfolioSidekick.ProcessingService;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.IntegrationTests
{
	internal class TestLogger(string checkForEndMessage) : ILogger<TimedHostedService>, IDisposable
	{
		public volatile bool IsTriggered = false;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return this;
		}

		public void Dispose()
		{
			// Empty
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			var message = formatter(state, exception);
			if (message != null && message.Contains(checkForEndMessage))
			{
				IsTriggered = true;
			}
		}
	}
}