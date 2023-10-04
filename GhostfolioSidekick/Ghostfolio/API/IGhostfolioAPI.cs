using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public interface IGhostfolioAPI
	{
		Task<Model.Account?> GetAccountByName(string name);

		public Task UpdateAccount(IEnumerable<Model.Account> accounts);

		Task<Model.Asset?> FindSymbolByISIN(string? identifier, Func<IEnumerable<Model.Asset>, Model.Asset?> selector = null);

		Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date);

		Task<Money?> GetMarketPrice(Model.Asset asset, DateTime date);
	}
}
