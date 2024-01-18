using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class MarketDataManager : IMarketDataManager
	{
		public MarketDataManager(
			)
		{

		}

		public Task<SymbolProfile?> FindSymbolByIdentifier(string[] identifiers, Currency? expectedCurrency, AssetClass[]? allowedAssetClass, AssetSubClass[]? allowedAssetSubClass, bool checkExternalDataProviders, bool includeIndexes)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MarketDataProfile>> GetMarketData()
		{
			throw new NotImplementedException();
		}
	}
}
