using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class CurrencyConvertion(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : ICurrencyConvertion
	{
		public async Task ConvertAll(Currency targetCurrency)
		{
			var snapShots = await databaseContext
				.CalculatedSnapshots
				.Where(x => x.TotalValue.Currency != targetCurrency || x.CurrentUnitPrice.Currency != targetCurrency || x.TotalInvested.Currency != targetCurrency)
				.ToListAsync();

			foreach (var snapShot in snapShots)
			{
				snapShot.TotalValue = await currencyExchange.ConvertMoney(snapShot.TotalValue, targetCurrency, snapShot.Date);
				snapShot.CurrentUnitPrice = await currencyExchange.ConvertMoney(snapShot.CurrentUnitPrice, targetCurrency, snapShot.Date);
				snapShot.TotalInvested = await currencyExchange.ConvertMoney(snapShot.TotalInvested, targetCurrency, snapShot.Date);
			}

			var balances = await databaseContext
					.Accounts
					.SelectMany(x => x.Balance)
					.Where(x => x.Money.Currency != targetCurrency)
					.ToListAsync();

			foreach (var balance in balances)
			{
				balance.Money = await currencyExchange.ConvertMoney(balance.Money, targetCurrency, DateOnly.FromDateTime(DateTime.Now));
			}

			await databaseContext.SaveChangesAsync();

		}
	}
}
