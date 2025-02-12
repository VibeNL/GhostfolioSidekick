using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface ICurrencyRepository
	{
		Task<IEnumerable<MarketData>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate);
	}
}
