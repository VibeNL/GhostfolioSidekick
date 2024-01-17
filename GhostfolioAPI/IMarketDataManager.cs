
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IMarketDataManager
	{
		Task<IEnumerable<MarketDataProfile>> GetMarketData();
	}
}
