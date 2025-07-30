using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface ICurrencyRepository
	{
		Task<IEnumerable<CurrencyExchangeRate>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate);
	}
}
