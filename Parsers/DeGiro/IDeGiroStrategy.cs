using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	internal interface IDeGiroStrategy
	{
		PartialActivityType? GetActivityType(DeGiroRecord record);

		decimal GetQuantity(DeGiroRecord record);

		decimal GetUnitPrice(DeGiroRecord record);

		Currency GetCurrency(DeGiroRecord record, ICurrencyMapper currencyMapper);

		void SetGenerateTransactionIdIfEmpty(DeGiroRecord record, DateTime recordDate);
	}
}
