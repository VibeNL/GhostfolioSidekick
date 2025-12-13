using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class AccountDataService(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IServerConfigurationService serverConfigurationService
	) : IAccountDataService
	{
		public Task<List<Account>> GetAccountInfo()
		{
			using var databaseContext = dbContextFactory.CreateDbContext();
			return databaseContext.Accounts
				.Include(a => a.Platform)
				.OrderBy(a => a.Name)
				.AsNoTracking()
				.ToListAsync();
		}

		public Task<List<Account>> GetAccountsAsync(string? symbolFilter, CancellationToken cancellationToken = default)
		{
			using var databaseContext = dbContextFactory.CreateDbContext();
			var query = databaseContext.Accounts
				.Include(a => a.Platform)
				.AsNoTracking()
				.OrderBy(a => a.Name)
				.AsQueryable();
			if (!string.IsNullOrWhiteSpace(symbolFilter))
			{
				query = query.Where(a => a.Activities.Any(h => h.Holding != null && h.Holding.SymbolProfiles.Any(s => s.Symbol == symbolFilter)));
			}

			return query.ToListAsync(cancellationToken);
		}

		public async Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			using var databaseContext = dbContextFactory.CreateDbContext();
			var snapShots = await databaseContext.CalculatedSnapshotPrimaryCurrencies
				.Where(s => s.Date >= startDate && s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					Date = g.Key.Date,
					Value = g.Sum(x => (double)x.TotalValue),
					Invested = g.Sum(x => (double)x.TotalInvested),
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

			return [.. result.OrderBy(x => x.Date).ThenBy(x => x.AccountId)];
		}

		public Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
		{
			using var databaseContext = dbContextFactory.CreateDbContext();
			return databaseContext.CalculatedSnapshotPrimaryCurrencies
				.MinAsync(s => s.Date, cancellationToken);
		}

		public Task<List<string>> GetSymbolProfilesAsync(int? accountFilter, CancellationToken cancellationToken = default)
		{
			using var databaseContext = dbContextFactory.CreateDbContext();
			if (!accountFilter.HasValue)
			{
				return databaseContext.SymbolProfiles
					.OrderBy(s => s.Symbol)
					.Select(s => s.Symbol)
					.ToListAsync(cancellationToken);
			}

			return databaseContext.Holdings
				.Where(x => x.Activities.Any(y => y.Account.Id == accountFilter))
				.SelectMany(x => x.SymbolProfiles)
				.OrderBy(s => s.Symbol)
				.Select(s => s.Symbol)
				.ToListAsync(cancellationToken);
		}
	}
}
