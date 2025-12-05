using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface IDividendRepository
	{
		Task<IList<Dividend>> Gather(SymbolProfile symbol);
	}
}
