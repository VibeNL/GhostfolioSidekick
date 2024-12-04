using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Database.Repository
{
	public class CurrencyExchange(IDbContextFactory<DatabaseContext> databaseContextFactory, ILogger<CurrencyExchange> logger) : ICurrencyExchange
	{
		public async Task<Money> ConvertMoney(Money money, Currency currency, DateOnly date)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();

			if (money.Currency == currency)
			{
				return money;
			}

			if (money.Currency.IsKnownPair(currency))
			{
				return new Money(currency, money.Times(money.Currency.GetKnownExchangeRate(currency)).Amount);
			}

			var searchDate = date;
			if (date.DayOfWeek == DayOfWeek.Saturday)
			{
				searchDate = date.AddDays(-1);
			}
			else if (date.DayOfWeek == DayOfWeek.Sunday)
			{
				searchDate = date.AddDays(-2);
			}

			var exchangeRate = await databaseContext.SymbolProfiles
								.Where(x => x.Symbol == $"{money.Currency.Symbol}{currency.Symbol}")
								.SelectMany(x => x.MarketData)
								.Where(x => x.Date == searchDate)
								.Select(x => x.Close)
								.AsNoTracking()
								.SingleOrDefaultAsync();

			if (exchangeRate == null)
			{
                logger.LogWarning("No exchange rate found for {FromCurrency} to {ToCurrency} on {SearchDate}. Using previous rate.", money.Currency, currency, searchDate);

				exchangeRate = await databaseContext.SymbolProfiles
									.Where(x => x.Symbol == $"{money.Currency.Symbol}{currency.Symbol}")
									.SelectMany(x => x.MarketData)
									.Where(x => x.Date < searchDate)
									.OrderByDescending(x => x.Date)
									.Select(x => x.Close)
									.AsNoTracking()
									.FirstOrDefaultAsync();

				if (exchangeRate == null)
				{
					logger.LogWarning("No exchange rate found for {FromCurrency} to {ToCurrency}. Using 1:1 rate.", money.Currency, currency);
					return new Money(currency, money.Amount);
				}
			}

			return new Money(currency, money.Times(exchangeRate.Amount).Amount);
		}
	}
}
