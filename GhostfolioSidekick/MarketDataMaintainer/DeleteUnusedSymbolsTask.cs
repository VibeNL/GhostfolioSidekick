using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	public class DeleteUnusedSymbolsTask : IScheduledWork
	{
		private readonly ILogger<DeleteUnusedSymbolsTask> logger;
		private readonly IMarketDataService marketDataManager;
		private readonly IApplicationSettings applicationSettings;

		public TaskPriority Priority => TaskPriority.DeleteUnusedSymbols;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public DeleteUnusedSymbolsTask(
			ILogger<DeleteUnusedSymbolsTask> logger,
			IMarketDataService marketDataManager,
			IApplicationSettings applicationSettings)
		{
			ArgumentNullException.ThrowIfNull(applicationSettings);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.marketDataManager = marketDataManager;
			this.applicationSettings = applicationSettings;
		}

		public async Task DoWork()
		{
			logger.LogInformation($"{nameof(DeleteUnusedSymbolsTask)} Starting to do work");

			try
			{
			
					await DeleteUnusedSymbols();
				
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogInformation($"{nameof(DeleteUnusedSymbolsTask)} Done");
		}

		private async Task DeleteUnusedSymbols()
		{
			var profiles = await marketDataManager.GetAllSymbolProfiles();
			foreach (var profile in from profile in profiles
									where profile.ActivitiesCount == 0 &&
										  (IsGeneratedSymbol(profile) ||applicationSettings.ConfigurationInstance.Settings.DeleteUnusedSymbols )
									select profile)
			{
				await marketDataManager.DeleteSymbol(profile);
			}

			static bool IsGeneratedSymbol(SymbolProfile assetProfile)
			{
				var guidRegex = new Regex("^(?:\\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\\}{0,1})$");
				return guidRegex.IsMatch(assetProfile.Symbol) && assetProfile.DataSource == Datasource.MANUAL;
			}
		}
	}
}
