using GhostfolioSidekick.FileImporter;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.MarketDataMaintainer.Actions;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class CreateManualSymbolTask : IScheduledWork
	{
		private readonly ILogger<FileImporterTask> logger;
		private readonly IGhostfolioAPI api;
		private readonly CreateManualSymbol action;

		public int Priority => 2;

		public CreateManualSymbolTask(
			ILogger<FileImporterTask> logger,
			IGhostfolioAPI api,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.api = api ?? throw new ArgumentNullException(nameof(api));

			action = new CreateManualSymbol(api, applicationSettings.ConfigurationInstance);
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(CreateManualSymbolTask)} Starting to do work");

			try
			{
				await action.ManageManualSymbols();
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				api.SetAllowAdmin(false);
			}

			logger.LogInformation($"{nameof(CreateManualSymbolTask)} Done");
		}
	}
}