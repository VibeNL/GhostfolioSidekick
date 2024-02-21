using System.Diagnostics.CodeAnalysis;

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
	}
}