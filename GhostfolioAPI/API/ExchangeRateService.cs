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
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
			this.memoryCache = memoryCache;
			this.logger = logger;
		}

		public async Task<decimal> GetConversionRate(Currency sourceCurrency, Currency targetCurrency, DateTime dateTime)
		{
			if (sourceCurrency == null || sourceCurrency.Symbol == targetCurrency.Symbol)
			{
				return 1;
			}

			var key = $"{nameof(ExchangeRateService)}{sourceCurrency.Symbol}{targetCurrency.Symbol}{dateTime.ToInvariantString()}";
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
				var token = stuff!.marketPrice.ToString();

				var rate = (decimal)decimal.Parse(token);
				memoryCache.Set(key, rate, CacheDuration.Short());

				return rate;
			}
			catch
			{
				logger.LogWarning($"Exchange rate not found for {sourceCurrency}-{targetCurrency.Symbol} on {dateTime}. Assuming rate of 1");
			}

			return 1;
		}
	}
}
