using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface IStockSplitRepository
	{
		Task<IEnumerable<StockSplit>> GetStockSplits(string symbol);
	}
}
