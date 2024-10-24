using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Market
{
	public record StockSplit(DateOnly Date, decimal BeforeSplit, decimal AfterSplit)
	{
	}
}
