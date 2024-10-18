using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IMarketDataRepository
	{
		Task<DateOnly> GetEarliestActivityDate(SymbolProfile symbol);

		Task<SymbolProfile?> GetSymbolProfileBySymbol(string symbolString);

		Task<IEnumerable<SymbolProfile>> GetSymbolProfiles();


		Task Store(SymbolProfile symbolProfile);
	}
}
