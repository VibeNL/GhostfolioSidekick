namespace GhostfolioSidekick.Model.Market
{
	public record CurrencyExchangeRate
	{
		public CurrencyExchangeRate(
			Money close,
			Money open,
			Money high,
			Money low,
			decimal tradingVolume,
			DateOnly date)
		{
			Close = close ?? throw new ArgumentNullException(nameof(close));
			Open = open ?? throw new ArgumentNullException(nameof(open));
			High = high ?? throw new ArgumentNullException(nameof(high));
			Low = low ?? throw new ArgumentNullException(nameof(low));
			TradingVolume = tradingVolume;
			Date = date;
		}

		public CurrencyExchangeRate() // EF Core
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

		public void CopyFrom(CurrencyExchangeRate marketData)
		{
			Close = marketData.Close;
			Open = marketData.Open;
			High = marketData.High;
			Low = marketData.Low;
			TradingVolume = marketData.TradingVolume;
			Date = marketData.Date;
		}
	}
}