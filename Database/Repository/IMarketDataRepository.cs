using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IMarketDataRepository
	{
		public IEnumerable<SymbolProfile> GetSymbols();
	}
}
