namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	public class MarketData
	{
		public DateTime Date { get; set; }

		public required string Symbol { get; set; }

		public decimal MarketPrice { get; set; }

		public required string DataSource { get; set; }

		public int ActivitiesCount { get; set; }
	}
}