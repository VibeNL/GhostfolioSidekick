namespace GhostfolioSidekick.Model
{
	public class MarketData
	{
		public MarketData(string symbol, string dataSource, decimal marketPrice, DateTime date)
		{
			Symbol = symbol;
			DataSource = dataSource;
			MarketPrice = marketPrice;
			Date = date;
		}

		public string Symbol { get; set; }

		public string DataSource { get; set; }

		public decimal MarketPrice { get; }

		public DateTime Date { get; }
	}
}