using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Database.Repository
{
	public interface ICurrencyExchange
	{
		Task<Money> ConvertMoney(Money money, Currency currency, DateOnly date);
	}
}
