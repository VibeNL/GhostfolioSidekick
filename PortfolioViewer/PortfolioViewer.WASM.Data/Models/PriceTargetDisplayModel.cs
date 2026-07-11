namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;

public class PriceTargetDisplayModel
{
	public string Symbol { get; set; } = default!;
	public string Name { get; set; } = default!;

	public decimal HighestTargetAmount { get; set; }
	public string HighestTargetCurrency { get; set; } = default!;

	public decimal AverageTargetAmount { get; set; }
	public string AverageTargetCurrency { get; set; } = default!;

	public decimal LowestTargetAmount { get; set; }
	public string LowestTargetCurrency { get; set; } = default!;

	public string Rating { get; set; } = default!;
	public int NumberOfBuys { get; set; }
	public int NumberOfHolds { get; set; }
	public int NumberOfSells { get; set; }
}
