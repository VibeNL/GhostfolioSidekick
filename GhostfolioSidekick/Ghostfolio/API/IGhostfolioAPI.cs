using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public interface IGhostfolioAPI
	{
		// Find a symbol.
		// Note: When a symbol does not yet exists, it is created!
		Task<SymbolProfile?> FindSymbolByIdentifier(
			string[] identifiers,
			Currency? expectedCurrency,
			AssetClass[]? expectedAssetClass,
			AssetSubClass[]? expectedAssetSubClass,
			bool checkExternalDataProviders = true,
			bool includeIndexes = false);

		Task<SymbolProfile?> FindSymbolByIdentifier(
			string identifier,
			Currency? expectedCurrency,
			AssetClass[]? expectedAssetClass,
			AssetSubClass[]? expectedAssetSubClass,
			bool checkExternalDataProviders = true,
			bool includeIndexes = false)
		{
			return FindSymbolByIdentifier(
				new[] { identifier },
				expectedCurrency,
				expectedAssetClass,
				expectedAssetSubClass,
				checkExternalDataProviders,
				includeIndexes);
		}

		Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date);

		Task<Money?> GetMarketPrice(SymbolProfile asset, DateTime date);

		Task<Account?> GetAccountByName(string name);

		Task<Platform?> GetPlatformByName(string name);

		Task UpdateAccount(Account account);

		Task<IEnumerable<MarketDataList>> GetMarketData();

		Task<MarketDataList> GetMarketData(string symbol, string dataSource);

		Task DeleteSymbol(SymbolProfile marketData);

		Task CreateSymbol(SymbolProfile asset);

		Task UpdateSymbol(SymbolProfile asset);

		Task<IEnumerable<Activity>> GetAllActivities();

		Task SetMarketPrice(SymbolProfile assetProfile, Money money);

		Task CreatePlatform(Platform platform);

		Task CreateAccount(Account account);

		Task GatherAllMarktData();

		Task AddAndRemoveDummyCurrency();

		void SetAllowAdmin(bool isallowed);

		void ClearCache();

		Task SetSymbolAsBenchmark(string symbol, string dataSource);
	}
}
