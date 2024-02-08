using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Model.Compare
{
	public interface IExchangeRateService
	{
		Task<decimal> GetConversionRate(Currency sourceCurrency, Currency targetCurrency, DateTime dateTime);
	}
}
