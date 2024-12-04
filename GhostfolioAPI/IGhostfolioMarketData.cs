using GhostfolioSidekick.GhostfolioAPI.Contract;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IGhostfolioMarketData
	{
		Task DeleteSymbol(SymbolProfile symbolProfile);

		Task<IEnumerable<SymbolProfile>> GetAllSymbolProfiles();

		Task<GenericInfo> GetBenchmarks();
	}
}
