namespace GhostfolioSidekick.Model
{
	public class Money
	{
		public Money(string currency, decimal? amount)
		{
			Currency = CurrencyHelper.ParseCurrency(currency);
			Amount = amount;
		}

		public Money(Currency currency, decimal? amount)
		{
			Currency = currency;
			Amount = amount;
		}

		public decimal? Amount { get; set; }

		public Currency Currency { get; set; }
	}
}