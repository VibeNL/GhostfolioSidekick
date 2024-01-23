using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public interface IExchangeRateService
	{
		Task<decimal> GetConversionRate(Currency currency, Currency targetCurrency, DateTime dateTime);
	}
}
