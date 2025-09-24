using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class AccountDataService(
		DatabaseContext databaseContext,
		IServerConfigurationService serverConfigurationService
	) : IAccountDataService
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
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			var snapShots = await databaseContext.CalculatedSnapshotPrimaryCurrencies
				.Where(s => s.Date >= startDate && s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					Date = g.Key.Date,
					Value = g.Sum(x => (double)x.TotalValue),
					Invested = g.Sum(x => (double)x.TotalValue),
					AccountId = g.Key.AccountId,
					Currency = serverConfigurationService.PrimaryCurrency.Symbol
				})
				.ToListAsync(cancellationToken);

			var balanceByAccount = await databaseContext.BalancePrimaryCurrencies
				.Where(s => s.Date >= startDate && s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					Date = g.Key.Date,
					Value = g.Min(x => (double)x.Money),
					AccountId = g.Key.AccountId,
				})
				.ToListAsync(cancellationToken);

			var result = new List<AccountValueHistoryPoint>();
			
			var join = from b in balanceByAccount
					   join s in snapShots on new { b.Date, b.AccountId } equals new { s.Date, s.AccountId } into bs
					   from s in bs.DefaultIfEmpty()
					   select new
					   {
						   b.Date,
						   Value = (decimal)(s?.Value ?? 0),
						   Invested = (decimal)(s?.Invested ?? 0),
						   b.AccountId,
						   Balance = (decimal)b.Value
					   };

			foreach (var item in join)
			{
				result.Add(new AccountValueHistoryPoint
				{
					Date = item.Date,
					AccountId = item.AccountId,
					TotalAssetValue = new Money(serverConfigurationService.PrimaryCurrency, item.Value),
					TotalInvested = new Money(serverConfigurationService.PrimaryCurrency, item.Invested),
					CashBalance = new Money(serverConfigurationService.PrimaryCurrency, item.Balance),
					TotalValue = new Money(serverConfigurationService.PrimaryCurrency, item.Value + item.Balance)
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
	}
}
