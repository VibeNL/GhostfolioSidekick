using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Market
{
	public class PriceTarget
	{
		public int Id { get; set; }

		public string Symbol { get; set; } = default!;

		public decimal HighestTargetPriceAmount { get; set; }

		public Currency HighestTargetCurrency { get; set; } = default!;

		public decimal AverageTargetPriceAmount { get; set; }

		public Currency AverageTargetCurrency { get; set; } = default!;

		public decimal LowestTargetPriceAmount { get; set; }

		public Currency LowestTargetCurrency { get; set; } = default!;

		public AnalystRating Rating { get; set; }

		public int NumberOfBuys { get; set; }

		public int NumberOfHolds { get; set; }

		public int NumberOfSells { get; set; }
	}
}
