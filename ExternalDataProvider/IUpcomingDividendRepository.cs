using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface IUpcomingDividendRepository
	{
		Task<IList<UpcomingDividend>> Gather(SymbolProfile symbol);
	}
}
