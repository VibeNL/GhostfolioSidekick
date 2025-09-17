using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class AccountDataService(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IAccountDataService
	{
		public Task<List<Account>> GetAccountInfo()
		{
			return databaseContext.Accounts
				.Include(a => a.Platform)
				.OrderBy(a => a.Name)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(
			Currency currency,
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			var snapShotsRaw = await databaseContext.CalculatedSnapshots
				.Where(s => s.Date >= startDate && s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId, s.HoldingAggregatedId })
				.Select(g => new
				{
					Date = g.Key.Date,
					Value = g.Min(x => (double)x.TotalValue.Amount),
					Invested = g.Max(x => (double)x.TotalValue.Amount),
					AccountId = g.Key.AccountId,
					Currency = g.FirstOrDefault().TotalValue.Currency.Symbol
				})
				.ToListAsync(cancellationToken);

			var snapshotsConverted = new List<(DateOnly Date, int AccountId, decimal Value, decimal Invested)>();
			foreach (var group in snapShotsRaw.GroupBy(s => new { s.Date, s.AccountId }))
			{
				var monies = group.Select(x => new Money(Currency.GetCurrency(x.Currency), (decimal)x.Value));
				var investedMonies = group.Select(x => new Money(Currency.GetCurrency(x.Currency), (decimal)x.Invested));
				
				var convertedValue = await ToSingleCurrencyAsync(monies, currency);
				var convertedInvested = await ToSingleCurrencyAsync(investedMonies, currency);
				
				snapshotsConverted.Add((group.Key.Date, group.Key.AccountId, convertedValue.Amount, convertedInvested.Amount));
			}

			var balanceByAccount = await databaseContext.Accounts
				.SelectMany(x => x.Balance)
				.Where(s => s.Date >= startDate && s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					Date = g.Key.Date,
					Value = g.Select(x => x.Money),
					AccountId = g.Key.AccountId,
				})
				.ToListAsync(cancellationToken);

			var result = new List<AccountValueHistoryPoint>();
			
			var join = from b in balanceByAccount
					   join s in snapshotsConverted on new { b.Date, b.AccountId } equals new { s.Date, s.AccountId } into bs
					   from s in bs.DefaultIfEmpty()
					   select new
					   {
						   b.Date,
						   Value = s.Value,
						   Invested = s.Invested,
						   b.AccountId,
						   Balance = b.Value
					   };

			foreach (var item in join)
			{
				var balance = item.Balance.Any() ? await ToSingleCurrencyAsync(item.Balance, currency) : new Money(currency, 0);
				
				result.Add(new AccountValueHistoryPoint
				{
					Date = item.Date,
					AccountId = item.AccountId,
					TotalValue = new Money(currency, item.Value),
					TotalInvested = new Money(currency, item.Invested),
					Balance = balance,
				});
			}

			return result.OrderBy(x => x.Date).ThenBy(x => x.AccountId).ToList();
		}

		public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
		{
			var minDate = await databaseContext.CalculatedSnapshots
				.OrderBy(s => s.Date)
				.Select(s => s.Date)
				.FirstOrDefaultAsync(cancellationToken);
			return minDate;
		}

		private async Task<Money> ToSingleCurrencyAsync(IEnumerable<Money> monies, Currency targetCurrency)
		{
			Money total = new(targetCurrency, 0);
			foreach (var money in monies)
			{
				if (money.Currency == targetCurrency)
				{
					total = total.Add(money);
				}
				else
				{
					var converted = await currencyExchange.ConvertMoney(money, targetCurrency, DateOnly.FromDateTime(DateTime.Now));
					total = total.Add(converted);
				}
			}

			return total;
		}
	}
}
