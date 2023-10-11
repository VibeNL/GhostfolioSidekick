using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public interface IGhostfolioAPI
	{
		Task<Account?> GetAccountByName(string name);

		public Task UpdateAccount(Account account);

		Task<Asset?> FindSymbolByISIN(string? identifier, Func<IEnumerable<Asset>, Asset?> selector = null);

		Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date);

		Task<Money?> GetMarketPrice(Asset asset, DateTime date);

		Task<IEnumerable<MarketDataInfo>> GetMarketDataInfo();

		Task DeleteMarketData(MarketDataInfo marketData);
	}
}
