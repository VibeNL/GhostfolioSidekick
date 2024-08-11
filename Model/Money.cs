namespace GhostfolioSidekick.Model
{
	public class Money
    {
        public decimal Amount { get; set; }

		public Currency Currency { get; set; }

		internal Money()
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

        public override bool Equals(object? obj)
		{
			return obj is Money money &&
				   Currency.Equals(money.Currency) &&
				   Amount == money.Amount;
		}

		override public int GetHashCode()
		{
			return HashCode.Combine(Currency, Amount);
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