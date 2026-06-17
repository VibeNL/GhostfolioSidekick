using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.IntegrationTests
{
	internal class TestLogger(string checkForEndMessage) : ILogger<TimedHostedService>, IDisposable
	{
		public volatile bool IsTriggered;

		public List<string> Messages { get; } = new();

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
			if (message != null)
			{
				Messages.Add(message);
				if (logLevel == LogLevel.Critical && exception != null)
				{
					Console.WriteLine($"[CRITICAL] {message} Exception: {exception.Message}");
				}
				if (logLevel == LogLevel.Error && exception != null)
				{
					Console.WriteLine($"[ERROR] {message} Exception: {exception.Message}");
				}
				if (message.Contains(checkForEndMessage))
				{
					IsTriggered = true;
				}
			}
		}
	}
}