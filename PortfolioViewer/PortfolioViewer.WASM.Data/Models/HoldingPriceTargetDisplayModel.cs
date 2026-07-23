using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	/// <summary>
	/// Combines a current holding with its analyst price target data for comparison.
	/// </summary>
	public class HoldingPriceTargetDisplayModel
	{
		public string Symbol { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public decimal Quantity { get; set; }
		public required Money CurrentPrice { get; set; }
		public decimal HighestTargetAmount { get; set; }
		public string HighestTargetCurrency { get; set; } = "USD";
		public decimal AverageTargetAmount { get; set; }
		public string AverageTargetCurrency { get; set; } = "USD";
		public decimal LowestTargetAmount { get; set; }
		public string LowestTargetCurrency { get; set; } = "USD";
		public string Rating { get; set; } = string.Empty;
		public int NumberOfBuys { get; set; }
		public int NumberOfHolds { get; set; }
		public int NumberOfSells { get; set; }

		/// <summary>
		/// Current price expressed as a percentage of the average target price (CurrentPrice / AverageTarget * 100).
		/// 100% means the target has been reached; values above 100% mean the target has already been passed.
		/// </summary>
		public decimal ProximityPercentage { get; set; }
	}
}
