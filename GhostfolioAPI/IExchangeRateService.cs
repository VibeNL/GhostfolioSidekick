using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IExchangeRateService
	{
		Task<decimal> GetConversionRate(Currency sourceCurrency, Currency targetCurrency, DateTime dateTime);
	}
}
