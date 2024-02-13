namespace GhostfolioSidekick.Model.Market
{
	public class MarketData(string symbol, string dataSource, decimal marketPrice, DateTime date)
	{
		public string Symbol { get; } = symbol;

		public string DataSource { get; } = dataSource;

		public decimal MarketPrice { get; } = marketPrice;

		public DateTime Date { get; } = date;
	}
}