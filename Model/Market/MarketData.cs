namespace GhostfolioSidekick.Model.Market
{
	public class MarketData
	{
		public MarketData(Money marketPrice, DateTime date)
		{
			MarketPrice = marketPrice;
			Date = date;
		}

		public Money MarketPrice { get; }

		public DateTime Date { get; }
	}
}