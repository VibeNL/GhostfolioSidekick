namespace GhostfolioSidekick.Model
{
	public class Money(decimal amount, Currency currency)
	{
		public decimal Amount { get; set; } = amount;

		public Currency Currency { get; set; } = currency;
	}
}