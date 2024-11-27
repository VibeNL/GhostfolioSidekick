using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Sync
{
	internal class CleanupGhostfolioTask(
			IGhostfolioMarketData ghostfolioMarketData,
			IApplicationSettings applicationSettings) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CleanupGhostfolio;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public async Task DoWork()
		{
			var symbols = await ghostfolioMarketData.GetAllSymbolProfiles();
			var benchmarks = await ghostfolioMarketData.GetBenchmarks();

			foreach (var profile in symbols.Where(x => x.ActivitiesCount == 0))
			{
				if (IsGeneratedSymbol(profile) || applicationSettings.ConfigurationInstance.Settings.DeleteUnusedSymbols)
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

		private static bool IsGeneratedSymbol(GhostfolioAPI.Contract.SymbolProfile assetProfile)
		{
			var guidRegex = new Regex("^(?:\\{{0,1}(?:[0-9a-fA-F]){8}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){4}-(?:[0-9a-fA-F]){12}\\}{0,1})$");
			return guidRegex.IsMatch(assetProfile.Symbol) && assetProfile.DataSource == Datasource.MANUAL;
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
