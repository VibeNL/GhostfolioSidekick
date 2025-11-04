using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API.Compare;

namespace GhostfolioSidekick.Sync
{
	internal class CleanupGhostfolioTask(
			IGhostfolioMarketData ghostfolioMarketData,
			IApplicationSettings applicationSettings) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CleanupGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => false;

		public string Name => "Cleanup Ghostfolio Unused Symbols";

		public async Task DoWork()
		{
			var symbols = await ghostfolioMarketData.GetAllSymbolProfiles();
			var benchmarks = await ghostfolioMarketData.GetBenchmarks();

			foreach (var profile in symbols.Where(x => x.ActivitiesCount == 0))
			{
				if (Utils.IsGeneratedSymbol(profile) || applicationSettings.ConfigurationInstance.Settings.DeleteUnusedSymbols)
				{
					// Exclude benchmarks and fear and greet index
					if (IsBenchmark(benchmarks, profile) || IsFearAndGreedIndex(profile))
					{
						continue;
					}

					await ghostfolioMarketData.DeleteSymbol(profile);
				}
			}
		}

		private static bool IsBenchmark(GhostfolioAPI.Contract.GenericInfo benchmarks, GhostfolioAPI.Contract.SymbolProfile profile)
		{
			return benchmarks.BenchMarks?.Any(y => y.Symbol == profile.Symbol) ?? false;
		}

		private static bool IsFearAndGreedIndex(GhostfolioAPI.Contract.SymbolProfile profile)
		{
			return profile.Symbol == "_GF_FEAR_AND_GREED_INDEX";
		}
	}
}
