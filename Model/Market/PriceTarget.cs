namespace GhostfolioSidekick.Model.Market
{
	public class PriceTarget
	{
		public int Id { get; set; }

		public Money HighestTargetPrice { get; set; } = default!;

		public Money AverageTargetPrice { get; set; } = default!;

		public Money LowestTargetPrice { get; set; } = default!;

		public AnalystRating Rating { get; set; }

		public int NumberOfBuys { get; set; }

		public int NumberOfHolds { get; set; }

		public int NumberOfSells { get; set; }
	}
}
