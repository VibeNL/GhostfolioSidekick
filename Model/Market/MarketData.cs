namespace GhostfolioSidekick.Model.Market
{
	public record MarketData
	{
		public MarketData(
			Money close,
			Money open,
			Money high,
			Money low,
			decimal tradingVolume,
			DateOnly date)
		{
			this.Close = close ?? throw new ArgumentNullException(nameof(close));
			this.Open = open ?? throw new ArgumentNullException(nameof(open));
			this.High = high ?? throw new ArgumentNullException(nameof(high));
			this.Low = low ?? throw new ArgumentNullException(nameof(low));
			this.TradingVolume = tradingVolume;
			this.Date = date;
		}

		public MarketData() // EF Core
		{
			Close = default!;
			Open = default!;
			High = default!;
			Low = default!;
		}

		public Money Close { get; set; }
		public Money Open { get; set; }
		public Money High { get; set; }
		public Money Low { get; set; }
		public decimal TradingVolume { get; set; }
		public DateOnly Date { get; set; }
	}
}