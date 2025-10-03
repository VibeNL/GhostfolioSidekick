
namespace GhostfolioSidekick.Model
{
	public record Money : IComparable<Money>, IEquatable<Money>
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


		public Money SafeDivide(Money money)
		{
			if (money.Currency != Currency)
			{
				throw new ArgumentException("Currencies do not match", nameof(money));
			}

			return SafeDivide(money.Amount);
		}

		public override string ToString()
		{
			return $"{Amount} {Currency}";
		}

		public static Money Sum(IEnumerable<Money> enumerable)
		{
			// Check if all currencies are the same
			if (!enumerable.Any())
			{
				return Zero(Currency.USD);
			}

			var firstCurrency = enumerable.First().Currency;
			decimal totalAmount = 0;
			foreach (var money in enumerable)
			{
				if (money.Currency != firstCurrency)
				{
					throw new ArgumentException("All Money objects must have the same currency", nameof(enumerable));
				}

				totalAmount += money.Amount;
			}

			return new Money(firstCurrency, totalAmount);
		}

		public static Money[] SumPerCurrency(IEnumerable<Money> enumerable)
		{
			var grouped = enumerable
				.GroupBy(m => m.Currency)
				.ToDictionary(g => g.Key, Sum);
			return [.. grouped.Values];
		}

		public int CompareTo(Money? other)
		{
			if (other is null)
			{
				return 1; // null is considered less than any Money instance
			}

			return Amount.CompareTo(other.Amount);
		}
	}
}