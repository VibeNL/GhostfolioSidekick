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
			var snapshotsConverted = snapShotsRaw
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(s => new
				{
					s.Key.Date,
					s.Key.AccountId,
					Value = ToSingleCurrency(s.Select(x => new Money(Currency.GetCurrency(x.Currency), (decimal)x.Value)), currency).Amount,
					Invested = ToSingleCurrency(s.Select(x => new Money(Currency.GetCurrency(x.Currency), (decimal)x.Invested)), currency).Amount,
				});

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

			var join = from s in snapshotsConverted
					   join b in balanceByAccount on new { s.Date, s.AccountId } equals new { b.Date, b.AccountId } into sb
					   from b in sb.DefaultIfEmpty()
					   select new
					   {
						   s.Date,
						   s.Value,
						   s.Invested,
						   s.AccountId,
						   Balance = b != null ? b.Value : Array.Empty<Money>()
					   };

			var result = join
				.Select(x => new AccountValueHistoryPoint
				{
					Date = x.Date,
					AccountId = x.AccountId,
					TotalValue = new Money(currency, x.Value),
					TotalInvested = new Money(currency, x.Invested),
					Balance = x.Balance.Any() ? ToSingleCurrency(x.Balance, currency) : new Money(currency, 0),
				})
				.OrderBy(x => x.Date)
				.ThenBy(x => x.AccountId)
				.ToList();

			return result;
		}

		public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
		{
			var minDate = await databaseContext.CalculatedSnapshots
				.OrderBy(s => s.Date)
				.Select(s => s.Date)
				.FirstOrDefaultAsync(cancellationToken);
			return minDate;
		}

		private Money ToSingleCurrency(IEnumerable<Money> monies, Currency targetCurrency)
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
					var converted = currencyExchange.ConvertMoney(money, targetCurrency, DateOnly.FromDateTime(DateTime.Now)).Result;
					total = total.Add(converted);
				}
			}

			return total;
		}
	}
}
