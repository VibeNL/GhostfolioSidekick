using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Compare;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ExchangeRateService : IExchangeRateService
	{
		private readonly RestCall restCall;
		private readonly IMemoryCache memoryCache;
		private readonly ILogger<ExchangeRateService> logger;

		public ExchangeRateService(RestCall restCall, IMemoryCache memoryCache, ILogger<ExchangeRateService> logger)
		{
			this.restCall = restCall;
			this.memoryCache = memoryCache;
			this.logger = logger;
		}

		public async Task<decimal> GetConversionRate(Currency? sourceCurrency, Currency? targetCurrency, DateTime dateTime)
		{
			if (sourceCurrency == null || targetCurrency == null || sourceCurrency.Symbol == targetCurrency.Symbol)
			{
				return 1;
			}

			var key = $"{nameof(ExchangeRateService)}{sourceCurrency.Symbol}{targetCurrency!.Symbol}{dateTime.ToInvariantString()}";
			if (memoryCache.TryGetValue(key, out decimal cacheValue))
			{
				return cacheValue;
			}

			try
			{

				var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency.Symbol}-{targetCurrency.Symbol}/{dateTime:yyyy-MM-dd}", true);
				if (content == null)
				{
					throw new NotSupportedException();
				}

				dynamic stuff = JsonConvert.DeserializeObject(content)!;
				var rate = (decimal)stuff.marketPrice;
				memoryCache.Set(key, rate, CacheDuration.Short());

				return rate;
			}
			catch(Exception ex)
			{
				logger.LogWarning(ex, "Exchange rate not found for {SourceCurrency}-{TargetCurrency} on {Date}. Assuming rate of 1", sourceCurrency, targetCurrency.Symbol, dateTime.ToShortDateString());
			}

			return 1;
		}
	}
}
