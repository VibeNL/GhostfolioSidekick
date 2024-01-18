
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IMarketDataManager
	{
		Task<IEnumerable<MarketDataProfile>> GetMarketData();

		Task<SymbolProfile?> FindSymbolByIdentifier(
			string[] identifiers,
			Currency? expectedCurrency,
			AssetClass[]? allowedAssetClass,
			AssetSubClass[]? allowedAssetSubClass,
			bool checkExternalDataProviders,
			bool includeIndexes);
	}
}
