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
			logger.LogDebug($"{nameof(DeleteUnusedSymbolsTask)} Starting to do work");

			try
			{
				await DeleteUnusedSymbols();
			}
			catch (NotAuthorizedException)
			{
				// Running against a managed instance?
				applicationSettings.AllowAdminCalls = false;
			}

			logger.LogDebug($"{nameof(DeleteUnusedSymbolsTask)} Done");
		}

		private async Task DeleteUnusedSymbols()
		{
			var profiles = await marketDataManager.GetAllSymbolProfiles();
			var benchmarks = await marketDataManager.GetInfo();
			foreach (var profile in profiles.Where(x => x.ActivitiesCount == 0))
			{
				if (IsGeneratedSymbol(profile) || applicationSettings.ConfigurationInstance.Settings.DeleteUnusedSymbols)
				{
					// Exclude benchmarks and fear and greet index
					if (IsBenchmark(benchmarks, profile) || IsFearAndGreedIndex(profile))
					{
						continue;
					}

					await marketDataManager.DeleteSymbol(profile);
				}
			}

			static bool IsGeneratedSymbol(SymbolProfile assetProfile)
			{
				var guidRegex = new Regex("^(?:\\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\\}{0,1})$");
				return guidRegex.IsMatch(assetProfile.Symbol) && assetProfile.DataSource == Datasource.MANUAL;
			}
		}

		private static bool IsBenchmark(GhostfolioAPI.Contract.GenericInfo benchmarks, SymbolProfile profile)
		{
			return benchmarks.BenchMarks?.Any(y => y.Symbol == profile.Symbol) ?? false;
		}

		private static bool IsFearAndGreedIndex(SymbolProfile profile)
		{
			return profile.Symbol == "_GF_FEAR_AND_GREED_INDEX";
		}
	}
}
