﻿namespace GhostfolioSidekick.Model
{
	public class Money
	{
		public Money(string currency, decimal amount, DateTime timeOfRecord)
		{
			Currency = CurrencyHelper.ParseCurrency(currency);
			Amount = amount;
			TimeOfRecord = timeOfRecord;

		}

		public Money(Currency currency, decimal amount, DateTime timeOfRecord)
		{
			Currency = currency;
			Amount = amount;
			TimeOfRecord = timeOfRecord;
		}

		public decimal Amount { get; set; }

		public Currency Currency { get; set; }

		public DateTime TimeOfRecord { get; set; }

		internal Money Absolute()
		{
			return new Money(Currency, Math.Abs(Amount), TimeOfRecord);
		}

		internal Money Negate()
		{
			return new Money(Currency, Amount * -1, TimeOfRecord);
		}

		internal Money Times(decimal quantity)
		{
			return new Money(Currency, Amount * quantity, TimeOfRecord);
		}
	}
}