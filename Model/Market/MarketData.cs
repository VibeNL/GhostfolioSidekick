namespace GhostfolioSidekick.Model.Market
{
	public class MarketData(string symbol, string dataSource, decimal marketPrice, DateTime date)
	{
		public string Symbol { get; set; } = symbol;

		public string DataSource { get; set; } = dataSource;

		public decimal MarketPrice { get; } = marketPrice;

		public DateTime Date { get; } = date;
	}
}