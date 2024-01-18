using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.ConsoleHelper
{
	internal class ConsoleLogger :
		ILogger<RestCall>,
		ILogger<MarketDataManager>,
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