namespace GhostfolioSidekick.Ghostfolio.Contract
{
	public class MarketData
	{
		public DateTime Date { get; set; }

		public string Symbol { get; set; }

		public decimal MarketPrice { get; set; }

		public string DataSource { get; set; }

		public int ActivitiesCount { get; set; }
	}
}