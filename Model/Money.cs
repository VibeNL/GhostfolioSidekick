namespace GhostfolioSidekick.Model
{
	public class Money(Currency currency, decimal amount)
	{
		public decimal Amount { get; set; } = amount;

		public Currency Currency { get; set; } = currency;

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

		public override string ToString()
		{
			return $"{Amount} {Currency}";
		}

		public static bool operator ==(Money a, Money? b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if ((a is null) || (b is null))
			{
				return false;
			}

			return a.Currency == b.Currency && a.Amount == b.Amount;
		}

		public static bool operator !=(Money a, Money? b)
		{
			return !(a == b);
		}

	}
}