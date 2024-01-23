using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ExchangeRateService : IExchangeRateService
	{
		private readonly RestCall restCall;
		private readonly ILogger<ExchangeRateService> logger;

		public ExchangeRateService(RestCall restCall, ILogger<ExchangeRateService> logger)
		{
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
			this.logger = logger;
		}

		public async Task<decimal> GetConversionRate(Currency sourceCurrency, Currency targetCurrency, DateTime dateTime)
		{
			if (sourceCurrency == null)
			{
				return 1;
			}

			try
			{

				var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency.Symbol}-{targetCurrency.Symbol}/{dateTime:yyyy-MM-dd}", CacheDuration.Short(), true);
				if (content == null)
				{
					throw new NotSupportedException();
				}

				dynamic stuff = JsonConvert.DeserializeObject(content)!;
				var token = stuff!.marketPrice.ToString();

				return (decimal)decimal.Parse(token);
			}
			catch
			{
				logger.LogWarning($"Exchange rate not found for {sourceCurrency}-{targetCurrency.Symbol} on {dateTime}. Assuming rate of 1");
			}

			return 1;
		}
	}
}
