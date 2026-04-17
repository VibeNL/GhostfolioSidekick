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
		public async Task<List<Account>> GetAccountInfo()
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync();
			return await databaseContext.Accounts
				.Include(a => a.Platform)
				.OrderBy(a => a.Name)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<Account>> GetAccountsAsync(string? symbolFilter, CancellationToken cancellationToken = default)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var query = databaseContext.Accounts
				.Include(a => a.Platform)
				.AsNoTracking()
				.OrderBy(a => a.Name)
				.AsQueryable();
			if (!string.IsNullOrWhiteSpace(symbolFilter))
			{
				query = query.Where(a => a.Activities.Any(h => h.Holding != null && h.Holding.SymbolProfiles.Any(s => s.Symbol == symbolFilter)));
			}

			return await query.ToListAsync(cancellationToken);
		}

		public async Task<List<AccountValueHistoryPoint>?> GetAccountValueHistoryAsync(
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var snapShots = await databaseContext.CalculatedSnapshots
				.Where(s => s.Date >= startDate && s.Date <= endDate)
				.Where(s => s.Holding != null && s.Holding.SymbolProfiles.Any())
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					g.Key.Date,
					Value = g.Sum(x => (double)x.TotalValue),
					Invested = g.Sum(x => (double)x.TotalInvested),
					g.Key.AccountId,
					Currency = serverConfigurationService.PrimaryCurrency.Symbol
				})
				.ToListAsync(cancellationToken);

			var balanceByAccount = await databaseContext.Balances
				.Where(s => s.Date >= startDate && s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					g.Key.Date,
					Value = g.Min(x => (double)x.Money.Amount),
					g.Key.AccountId,
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

		public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var minDate = await databaseContext.Activities
				.MinAsync(s => s.Date, cancellationToken);
			return DateOnly.FromDateTime(minDate);
		}

		public async Task<List<string>> GetSymbolProfilesAsync(int? accountFilter, CancellationToken cancellationToken = default)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			if (!accountFilter.HasValue)
			{
				return await databaseContext.SymbolProfiles
					.OrderBy(s => s.Symbol)
					.Select(s => s.Symbol)
					.ToListAsync(cancellationToken);
			}

			return await databaseContext.Holdings
				.Where(x => x.Activities.Any(y => y.Account.Id == accountFilter))
				.SelectMany(x => x.SymbolProfiles)
				.OrderBy(s => s.Symbol)
				.Select(s => s.Symbol)
				.ToListAsync(cancellationToken);
		}

		public async Task<List<TaxReportRow>> GetTaxReportAsync(CancellationToken cancellationToken = default)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			var accounts = await databaseContext.Accounts
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			var accountById = accounts.ToDictionary(a => a.Id, a => a.Name);

			// Determine all years for which data exists
			var snapshotYears = await databaseContext.CalculatedSnapshots
				.Select(s => s.Date.Year)
				.Distinct()
				.ToListAsync(cancellationToken);

			var balanceYears = await databaseContext.Balances
				.Select(b => b.Date.Year)
				.Distinct()
				.ToListAsync(cancellationToken);

			var years = snapshotYears.Union(balanceYears).Distinct().OrderBy(y => y).ToList();

			if (years.Count == 0)
				return [];

			var today = DateOnly.FromDateTime(DateTime.Today);
			var targetDates = years
				.SelectMany(y => new[]
				{
					new DateOnly(y, 1, 1),
					y == today.Year ? today : new DateOnly(y, 12, 31)
				})
				.ToList();

			var maxTargetDate = targetDates.Max();

			// Load all snapshots up to the last target date (no year restriction —
			// closest snapshot before Jan 1 might be in a prior year)
			var snapshots = await databaseContext.CalculatedSnapshots
				.Where(s => s.Date <= maxTargetDate)
				.Where(s => s.Holding != null && s.Holding.SymbolProfiles.Any())
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					g.Key.Date,
					g.Key.AccountId,
					TotalValue = g.Sum(x => x.TotalValue)
				})
				.ToListAsync(cancellationToken);

			// Load all balances up to the last target date — no year restriction so that
			// e.g. a Dec 31 balance is correctly picked up as the closest value for Jan 1
			var balances = await databaseContext.Balances
				.Where(b => b.Date <= maxTargetDate)
				.GroupBy(b => new { b.Date, b.AccountId })
				.Select(g => new
				{
					g.Key.Date,
					AccountId = g.Key.AccountId,
					Amount = g.Min(x => x.Money.Amount)
				})
				.ToListAsync(cancellationToken);

			var result = new List<TaxReportRow>();
			var currency = serverConfigurationService.PrimaryCurrency;

			foreach (var targetDate in targetDates)
			{
				// For each account find the most recent snapshot on or before targetDate
				var relevantSnapshots = snapshots
					.Where(s => s.Date <= targetDate)
					.GroupBy(s => s.AccountId)
					.Select(g => g.OrderByDescending(s => s.Date).First())
					.ToList();

				// For each account find the most recent balance on or before targetDate
				var relevantBalances = balances
					.Where(b => b.Date <= targetDate)
					.GroupBy(b => b.AccountId)
					.Select(g => g.OrderByDescending(b => b.Date).First())
					.ToList();

				var allAccountIds = relevantSnapshots.Select(s => s.AccountId)
					.Union(relevantBalances.Select(b => b.AccountId))
					.Distinct();

				foreach (var accountId in allAccountIds)
				{
					var assetValue = relevantSnapshots
						.Where(s => s.AccountId == accountId)
						.Sum(s => s.TotalValue);

					var cashBalance = relevantBalances
						.Where(b => b.AccountId == accountId)
						.Sum(b => b.Amount);

					result.Add(new TaxReportRow
					{
						Year = targetDate.Year,
						Date = targetDate,
						AccountId = accountId,
						AccountName = accountById.TryGetValue(accountId, out var name) ? name : $"Account {accountId}",
						AssetValue = new Money(currency, assetValue),
						CashBalance = new Money(currency, cashBalance),
						TotalValue = new Money(currency, assetValue + cashBalance)
					});
				}
			}

			return [.. result.OrderBy(r => r.Year).ThenBy(r => r.Date).ThenBy(r => r.AccountName)];
		}
	}
}
