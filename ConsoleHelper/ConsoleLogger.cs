using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.ConsoleHelper
{
	internal class ConsoleLogger :
		ILogger<RestCall>,
		ILogger<MarketDataService>,
		ILogger<AccountService>,
		ILogger<ActivitiesService>,
		ILogger<FileImporterTask>,
		ILogger<DisplayInformationTask>,
		IDisposable
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return this;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			Console.WriteLine(formatter(state, exception));
		}

		protected virtual void Dispose(bool disposing)
		{
		}
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}