using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	public class CurrencyRepository(ILogger<CurrencyRepository> logger) : ICurrencyRepository
	{
		public async Task<IEnumerable<MarketData>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			// You should be able to query data from various markets including US, HK, TW
			// The startTime & endTime here defaults to EST timezone
			var history = await YahooFinanceApi.Yahoo.GetHistoricalAsync("USDEUR=X", new DateTime(2016, 1, 1), new DateTime(2016, 7, 1), Period.Daily);

			foreach (var candle in history)
			{
				logger.LogDebug($"DateTime: {candle.DateTime}, Open: {candle.Open}, High: {candle.High}, Low: {candle.Low}, Close: {candle.Close}, Volume: {candle.Volume}, AdjustedClose: {candle.AdjustedClose}");
			}

			return null;
		}
	}
}
