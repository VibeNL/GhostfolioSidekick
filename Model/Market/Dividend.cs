namespace GhostfolioSidekick.Model.Market
{
	public record Dividend
	{
		public int Id { get; set; }

		public DateOnly ExDividendDate { get; set; }

		public DateOnly PaymentDate { get; set; }

		public DividendType DividendType { get; set; }

		public DividendState DividendState { get; set; }

		public Money Amount { get; set; } = default!;
	}
}
