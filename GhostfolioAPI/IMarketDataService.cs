
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Market;
//using GhostfolioSidekick.Model.Symbols;

//namespace GhostfolioSidekick.GhostfolioAPI
//{
//	public interface IMarketDataService
//	{
//		Task<IEnumerable<SymbolProfile>> GetAllSymbolProfiles();

//		Task<MarketDataProfile> GetMarketData(string symbol, string dataSource);

//		Task<SymbolProfile?> FindSymbolByIdentifier(
//			string[] identifiers,
//			Currency? expectedCurrency,
//			AssetClass[]? allowedAssetClass,
//			AssetSubClass[]? allowedAssetSubClass,
//			bool checkExternalDataProviders,
//			bool includeIndexes);

//		Task SetMarketPrice(SymbolProfile symbolProfile, Money money, DateTime dateTime);

//		Task CreateSymbol(SymbolProfile symbolProfile);

//		Task UpdateSymbol(SymbolProfile symbolProfile);

//		Task DeleteSymbol(SymbolProfile symbolProfile);

//		Task SetSymbolAsBenchmark(SymbolProfile symbolProfile);

//		Task GatherAllMarktData();
//		Task<Contract.GenericInfo> GetInfo();
//	}
//}
