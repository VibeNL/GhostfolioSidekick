using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.BrokerAPIs
{
	public interface IApiBrokerImporter
	{
		string BrokerType { get; }

		Task SyncAsync(
			string accountName,
			string outputDirectory,
			Dictionary<string, string> options,
			ILogger logger,
			CancellationToken cancellationToken = default);
	}
}
