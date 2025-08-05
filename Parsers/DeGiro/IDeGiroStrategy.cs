using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	internal interface IDeGiroStrategy
	{
		PartialActivityType? GetActivityType(DeGiroRecord record);

		decimal GetQuantity(DeGiroRecord record);

		decimal GetUnitPrice(DeGiroRecord record);

		decimal? GetTotal(DeGiroRecord record);

		decimal GetBalance(DeGiroRecord record);

		Currency GetCurrency(DeGiroRecord record, ICurrencyMapper currencyMapper);

		void SetGenerateTransactionIdIfEmpty(DeGiroRecord record, DateTime recordDate);
	}
}
