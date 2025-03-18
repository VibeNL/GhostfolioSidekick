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
			try
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

				var searchSourceCurrency = sourceCurrency.GetSourceCurrency();
				var searchTargetCurrency = targetCurrency.GetSourceCurrency();

				// If the currencies are the same, return 1
				if (searchSourceCurrency == searchTargetCurrency)
				{
					return searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
				}

				// Get the exchange rate from the database
				using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
				exchangeRate = await databaseContext.SymbolProfiles
									.Where(x => x.Symbol == $"{searchSourceCurrency.Item1.Symbol}{searchTargetCurrency.Item1.Symbol}")
									.SelectMany(x => x.MarketData)
									.Where(x => x.Date == searchDate)
									.Select(x => x.Close.Amount)
									.FirstOrDefaultAsync();

				if (exchangeRate == 0)
				{
					// Use the last known value. Mayby a holliyday or weekend?
					exchangeRate = await databaseContext.SymbolProfiles
										.Where(x => x.Symbol == $"{searchSourceCurrency.Item1.Symbol}{searchTargetCurrency.Item1.Symbol}")
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

				return exchangeRate * searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error occurred while getting exchange rate for {FromCurrency} to {ToCurrency} on {Date}.", sourceCurrency, targetCurrency, searchDate);
				throw;
			}
		}
	}
}
