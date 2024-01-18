using GhostfolioSidekick;
using GhostfolioSidekick.FileImporter;
using Microsoft.Extensions.Logging;

namespace ConsoleHelper
{
	internal class ConsoleLogger :
		ILogger<FileImporterTask>,
		ILogger<DisplayInformationTask>,
		IDisposable
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return this;
		}

		public void Dispose()
		{
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			Console.WriteLine(formatter(state, exception));
		}
	}
}