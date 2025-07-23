using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Database.Repository
{
	public interface ICurrencyExchange : IDisposable
	{
		Task<Money> ConvertMoney(Money money, Currency currency, DateOnly date);
		Task PreloadAllExchangeRates();
		void ClearPreloadedCache();
	}
}
