namespace GhostfolioSidekick.PortfolioViewer.Model
{
	public class StockSplit
	{
		public DateOnly Date { get; set; }
		public decimal BeforeSplit { get; set; }
		public decimal AfterSplit { get; set; }
	}
}
