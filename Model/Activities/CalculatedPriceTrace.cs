namespace GhostfolioSidekick.Model.Activities
{
	public record CalculatedPriceTrace
	{
		public CalculatedPriceTrace()
		{
			// EF Core
			NewPrice = default!;
		}

		public CalculatedPriceTrace(string source, Money price)
		{
			Reason = source;
			NewPrice = price;
		}

		public string Reason { get; set; } = string.Empty;

		public Money NewPrice { get; set; }
	}
}