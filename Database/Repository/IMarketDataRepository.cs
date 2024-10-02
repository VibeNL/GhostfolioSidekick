using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IMarketDataRepository
	{
		public IEnumerable<SymbolProfile> GetSymbols();


		Task StoreAll(IEnumerable<MarketData> data);
	}
}
