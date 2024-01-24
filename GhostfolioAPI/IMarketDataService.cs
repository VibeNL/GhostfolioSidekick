﻿
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IMarketDataService
	{
		Task<IEnumerable<MarketDataProfile>> GetMarketData(bool filterBenchmarks = true);

		Task<SymbolProfile?> FindSymbolByIdentifier(
			string[] identifiers,
			Currency? expectedCurrency,
			AssetClass[]? allowedAssetClass,
			AssetSubClass[]? allowedAssetSubClass,
			bool checkExternalDataProviders,
			bool includeIndexes);

		Task CreateSymbol(SymbolProfile symbolProfile);

		Task UpdateSymbol(SymbolProfile symbolProfile);

		Task SetMarketPrice(SymbolProfile assetProfile, Money money);
	}
}
