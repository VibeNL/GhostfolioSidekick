using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Database.Repository
{
	public class CurrencyExchange(IDbContextFactory<DatabaseContext> databaseContextFactory, ILogger<CurrencyExchange> logger) : ICurrencyExchange
	{
		public async Task<Money> ConvertMoney(Money money, Currency currency, DateOnly date)
		{
			var sourceCurrency = money.Currency;
			var targetCurrency = currency;
			var exchangeRate = await GetExchangeRate(date, sourceCurrency, targetCurrency);

			return new Money(currency, money.Times(exchangeRate).Amount);
		}

		private async Task<decimal> GetExchangeRate(DateOnly searchDate, Currency sourceCurrency, Currency targetCurrency)
		{
			// If the currencies are the same, return 1
			if (sourceCurrency == targetCurrency)
			{
				return 1;
			}

			// If the exchange rate is known, return it
			var exchangeRate = sourceCurrency.GetKnownExchangeRate(targetCurrency);
			if (exchangeRate != 0)
			{
				return exchangeRate;
			}

			var factor = 1m;
			var searchSourceCurrency = Currency.MapToStatics(sourceCurrency);
			var searchTargetCurrency = Currency.MapToStatics(targetCurrency);

			// If the source or target currency is a derived currency, get the source currency
			if (searchSourceCurrency.SourceCurrency != null)
			{
				factor = 1 / searchSourceCurrency.Factor; // Get the factor before overriding the currency
				searchSourceCurrency = searchSourceCurrency.SourceCurrency;
			}

			// If the source or target currency is a derived currency, get the source currency
			if (searchTargetCurrency.SourceCurrency != null)
			{
				factor *= searchTargetCurrency.Factor; // Invert the factor before overriding the currency
				searchTargetCurrency = searchTargetCurrency.SourceCurrency;
			}

			// If the currencies are the same, return 1
			if (searchSourceCurrency == searchTargetCurrency)
			{
				return 1;
			}

			// Get the exchange rate from the database
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			exchangeRate = await databaseContext.SymbolProfiles
								.Where(x => x.Symbol == $"{searchSourceCurrency.Symbol}{searchTargetCurrency.Symbol}")
								.SelectMany(x => x.MarketData)
								.Where(x => x.Date == searchDate)
								.Select(x => x.Close.Amount)
								.FirstOrDefaultAsync();

			if (exchangeRate == 0)
			{
				// Use the last known value. Mayby a holliyday or weekend?
				exchangeRate = await databaseContext.SymbolProfiles
									.Where(x => x.Symbol == $"{searchSourceCurrency.Symbol}{searchTargetCurrency.Symbol}")
									.SelectMany(x => x.MarketData)
									.Where(x => x.Date < searchDate)
									.OrderByDescending(x => x.Date)
									.Select(x => x.Close.Amount)
									.FirstOrDefaultAsync();

				if (exchangeRate == 0)
				{
					logger.LogWarning("No exchange rate found for {FromCurrency} to {ToCurrency}. Using 1:1 rate.", sourceCurrency, targetCurrency);
					return 1;
				}
			}

			return exchangeRate * factor;
		}
	}
}
