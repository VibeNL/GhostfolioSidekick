using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IMarketDataRepository
	{
		Task<SymbolProfile?> GetSymbolProfileBySymbol(string symbolString);

		Task<IEnumerable<SymbolProfile>> GetSymbolProfiles();


		Task Store(SymbolProfile symbolProfile);
	}
}
