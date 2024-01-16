using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Market
{
	public class MarketData(string symbol, Datasource dataSource, decimal marketPrice, DateTime date)
	{
		public string Symbol { get; set; } = symbol;

		public Datasource DataSource { get; set; } = dataSource;

		public decimal MarketPrice { get; } = marketPrice;

		public DateTime Date { get; } = date;
	}
}