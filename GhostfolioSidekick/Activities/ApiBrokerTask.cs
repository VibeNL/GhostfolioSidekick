using GhostfolioSidekick.BrokerAPIs;
using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Activities
{
	public class ApiBrokerTask(
		IApplicationSettings settings,
		IEnumerable<IApiBrokerImporter> importers) : IScheduledWork
	{
		private readonly string _fileLocation = settings.FileImporterPath;

		public TaskPriority Priority => TaskPriority.ApiBroker;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "API Broker Importer";

		public async Task DoWork(ILogger logger)
		{
			var connections = settings.ConfigurationInstance.BrokerApiConnections;
			if (connections == null || connections.Length == 0)
			{
				logger.LogDebug("{Name}: no API broker connections configured", nameof(ApiBrokerTask));
				return;
			}

			logger.LogDebug("{Name}: starting", nameof(ApiBrokerTask));

			foreach (var connection in connections)
			{
				var importer = importers.FirstOrDefault(i =>
					string.Equals(i.BrokerType, connection.Type, StringComparison.OrdinalIgnoreCase));

				if (importer == null)
				{
					logger.LogWarning("{Name}: no importer found for broker type '{Type}'", nameof(ApiBrokerTask), connection.Type);
					continue;
				}

				var outputDirectory = Path.Combine(_fileLocation, connection.AccountName);
				Directory.CreateDirectory(outputDirectory);

				logger.LogInformation("{Name}: syncing account '{AccountName}' via '{Type}'",
					nameof(ApiBrokerTask), connection.AccountName, connection.Type);

				await importer.SyncAsync(
					connection.AccountName,
					outputDirectory,
					connection.Options ?? new Dictionary<string, string>(),
					logger);
			}

			logger.LogDebug("{Name}: done", nameof(ApiBrokerTask));
		}
	}
}
