using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public interface IGhostfolioAPI
	{
		Task<SymbolProfile?> FindSymbolByIdentifier(
			string?[] identifiers,
			Currency? expectedCurrency,
			AssetClass?[] expectedAssetClass,
			AssetSubClass?[] expectedAssetSubClass);

		Task<SymbolProfile?> FindSymbolByIdentifier(
			string? identifier,
			Currency? expectedCurrency,
			AssetClass?[] expectedAssetClass,
			AssetSubClass?[] expectedAssetSubClass)
		{
			return FindSymbolByIdentifier(new[] { identifier }, expectedCurrency, expectedAssetClass, expectedAssetSubClass);
		}

		Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date);

		Task<Money?> GetMarketPrice(SymbolProfile asset, DateTime date);

		Task<Account?> GetAccountByName(string name);

		Task<Platform?> GetPlatformByName(string name);

		Task UpdateAccount(Account account);

		Task<IEnumerable<MarketDataList>> GetMarketData();

		Task<MarketDataList> GetMarketData(string symbol, string dataSource);

		Task UpdateMarketData(SymbolProfile marketData);

		Task DeleteSymbol(SymbolProfile marketData);

		Task CreateManualSymbol(SymbolProfile asset);

		Task<IEnumerable<Activity>> GetAllActivities();

		Task SetMarketPrice(SymbolProfile assetProfile, Money money);

		Task CreatePlatform(Platform platform);

		Task CreateAccount(Account account);

		Task GatherAllMarktData();

		Task AddAndRemoveDummyCurrency();
	}
}
