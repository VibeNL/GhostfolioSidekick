namespace GhostfolioSidekick.Model.Market
{
	public record CurrencyExchangeRate
	{
		public CurrencyExchangeRate(
			Currency currency,
			decimal close,
			decimal open,
			decimal high,
			decimal low,
			decimal tradingVolume,
			DateOnly date)
		{
			Currency = currency;
			Close = close;
			Open = open;
			High = high;
			Low = low;
			TradingVolume = tradingVolume;
			Date = date;
		}

		public CurrencyExchangeRate() // EF Core
		{
			Currency = default!;
			Close = default!;
			Open = default!;
			High = default!;
			Low = default!;
		}

		public Currency Currency { get; set; }
		public decimal Close { get; set; }
		public decimal Open { get; set; }
		public decimal High { get; set; }
		public decimal Low { get; set; }
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