namespace GhostfolioSidekick.Model.Market
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

		public string Symbol { get; }

		public string DataSource { get; }

		public decimal MarketPrice { get; }

		public DateTime Date { get; }
	}
}