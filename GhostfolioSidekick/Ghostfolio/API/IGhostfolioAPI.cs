namespace GhostfolioSidekick.Ghostfolio.API
{
	public interface IGhostfolioAPI
	{
		Task<Asset> FindSymbolByISIN(string? identifier, Func<IEnumerable<Asset>, Asset> selector = null);

		Task<decimal> GetExchangeRate(string sourceCurrency, string targetCurrency, DateTime date);

		Task<decimal> GetMarketPrice(Asset asset, DateTime date);

		Task<Account> GetAccountByName(string name);

		public Task UpdateOrders(IEnumerable<Activity> orders);
	}
}
