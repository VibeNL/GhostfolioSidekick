namespace GhostfolioSidekick.Model.Activities
{
	public record CalculatedPriceTrace
	{
		public CalculatedPriceTrace()
		{
			// EF Core
			NewPrice = default!;
		}

		public CalculatedPriceTrace(string source, decimal? quantity, Money? price)
		{
			Reason = source;
			NewQuantity = quantity;
			NewPrice = price;
		}

		public string Reason { get; set; } = string.Empty;

		public decimal? NewQuantity { get; }
		
		public Money? NewPrice { get; set; }

		public long ActivityId { get; set; }
	}
}