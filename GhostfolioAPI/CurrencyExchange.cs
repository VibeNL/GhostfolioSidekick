using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class CurrencyExchange(IDbContextFactory<DatabaseContext> databaseContextFactory) : ICurrencyExchange
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

			var exchangeRate = await databaseContext.SymbolProfiles
								.Where(x => x.Symbol == $"{money.Currency.Symbol}{currency.Symbol}")
								.SelectMany(x => x.MarketData)
								.Where(x => x.Date == date)
								.Select(x => x.Close)
								.AsNoTracking()
								.SingleAsync();

			return new Money(currency, money.Times(exchangeRate.Amount).Amount);
		}
	}
}
