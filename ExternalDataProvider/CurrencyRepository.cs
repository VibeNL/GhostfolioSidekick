using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using Microsoft.Extensions.Logging;
using Polly;
using YahooFinanceApi;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public class CurrencyRepository : ICurrencyRepository
	{
		private readonly Policy policy;
		private readonly ILogger<CurrencyRepository> logger;

		public CurrencyRepository(ILogger<CurrencyRepository> logger)
		{
			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetry(5, x => TimeSpan.FromSeconds(60), (exception, timeSpan, retryCount, context) =>
				{
					logger.LogWarning("The request failed");
				});

			policy = retryPolicy;
			this.logger = logger;
		}

		public async Task<IEnumerable<MarketData>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			try
			{
				var history = await Yahoo.GetHistoricalAsync("AAPL", new DateTime(2016, 1, 1), new DateTime(2016, 7, 1), Period.Daily);

				foreach (var candle in history)
				{
					Console.WriteLine($"DateTime: {candle.DateTime}, Open: {candle.Open}, High: {candle.High}, Low: {candle.Low}, Close: {candle.Close}, Volume: {candle.Volume}, AdjustedClose: {candle.AdjustedClose}");
				}

				return [];
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to get currency history");
				return [];
			}
		}
	}
}
