using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public interface IGhostfolioAPI
	{
		Task<Asset?> FindSymbolByIdentifier(string? identifier, Func<IEnumerable<Asset>, Asset?> selector = null);

		Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date);

		Task<Money?> GetMarketPrice(Asset asset, DateTime date);

		Task<Account?> GetAccountByName(string name);

		Task UpdateAccount(Account account);

		Task<IEnumerable<MarketData>> GetMarketDataInfo();

		Task<MarketData> GetMarketData(MarketData marketDataInfo);

		Task UpdateMarketData(MarketData marketData);

		Task DeleteSymbol(MarketData marketData);

		Task CreateManualSymbol(Asset asset);
	}
}
