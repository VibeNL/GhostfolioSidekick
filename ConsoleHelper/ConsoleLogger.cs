using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using Microsoft.Extensions.Logging;

namespace ConsoleHelper
{
    internal class ConsoleLogger : ILogger<GhostfolioAPI>, ILogger<FileImporterTask>, IDisposable
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