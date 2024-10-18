namespace GhostfolioSidekick.Model
{
	public record Money
    {
        public decimal Amount { get; set; }

		public Currency Currency { get; set; }

		public Money()
		{
			// EF Core
			Amount = 0;
			Currency = Currency.USD;
		}

		public Money(Currency currency, decimal amount)
        {
			Amount = amount;
			Currency = currency;
		}

        public Money Times(decimal amount)
		{
			return new Money(Currency, Amount * amount);
		}

		public Money SafeDivide(decimal amount)
		{
			if (amount == 0)
			{
				return new Money(Currency, 0);
			}

			return new Money(Currency, Amount / amount);
		}

		public override string ToString()
		{
			return $"{Amount} {Currency}";
		}
	}
}