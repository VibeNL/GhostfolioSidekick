namespace GhostfolioSidekick.Model
{
	public record Money
	{
		public decimal Amount { get; set; }

		public Currency Currency { get; set; }

		public static Money Zero(Currency currency) => new(currency, 0);

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

		public Money Add(Money money)
		{
			if (money.Currency != Currency)
			{
				throw new ArgumentException("Currencies do not match", nameof(money));
			}

			return new Money(Currency, Amount + money.Amount);
		}

		public Money Subtract(Money money)
		{
			if (money.Currency != Currency)
			{
				throw new ArgumentException("Currencies do not match", nameof(money));
			}
			return new Money(Currency, Amount - money.Amount);
		}

		public Money SafeDivide(decimal amount)
		{
			if (amount <= Constants.Epsilon)
			{
				return new Money(Currency, 0);
			}

			try
			{
				return new Money(Currency, Amount / amount);
			}
			catch (Exception ex)
			{
				// Log the exception or handle it as needed
				Console.WriteLine($"Error during division: {ex.Message}");
				return new Money(Currency, 0);
			}
		}

		public override string ToString()
		{
			return $"{Amount} {Currency}";
		}
	}
}