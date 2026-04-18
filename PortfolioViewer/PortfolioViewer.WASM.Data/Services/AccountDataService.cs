using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class AccountDataService(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IServerConfigurationService serverConfigurationService,
		ITaxReportCacheService taxReportCacheService
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

			// Fetch all records up to endDate (not filtered by startDate) so we can
			// forward-fill accounts whose last transaction predates startDate.
			var snapShots = await databaseContext.CalculatedSnapshots
				.Where(s => s.Date <= endDate)
				.Where(s => s.Holding != null && s.Holding.SymbolProfiles.Any())
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					g.Key.Date,
					Value = g.Sum(x => (double)x.TotalValue),
					Invested = g.Sum(x => (double)x.TotalInvested),
					g.Key.AccountId,
				})
				.ToListAsync(cancellationToken);

			var allBalances = await databaseContext.Balances
				.Where(s => s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					g.Key.Date,
					Value = g.Min(x => (double)x.Money.Amount),
					g.Key.AccountId,
				})
				.ToListAsync(cancellationToken);

			// Group sparse records by account for efficient forward-fill lookups.
			var balancesByAccount = allBalances
				.GroupBy(b => b.AccountId)
				.ToDictionary(g => g.Key, g => g.OrderBy(b => b.Date).ToList());

			var snapshotsByAccount = snapShots
				.GroupBy(s => s.AccountId)
				.ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).ToList());

			var allAccountIds = balancesByAccount.Keys.Union(snapshotsByAccount.Keys).Distinct();

			var result = new List<AccountValueHistoryPoint>();
			var currency = serverConfigurationService.PrimaryCurrency;

			foreach (var accountId in allAccountIds)
			{
				balancesByAccount.TryGetValue(accountId, out var accountBalances);
				snapshotsByAccount.TryGetValue(accountId, out var accountSnapshots);

				// Determine the first date this account has any data.
				var firstBalanceDate = accountBalances?.FirstOrDefault()?.Date;
				var firstSnapshotDate = accountSnapshots?.FirstOrDefault()?.Date;
				var firstDate = firstBalanceDate.HasValue && firstSnapshotDate.HasValue
					? (firstBalanceDate.Value < firstSnapshotDate.Value ? firstBalanceDate.Value : firstSnapshotDate.Value)
					: firstBalanceDate ?? firstSnapshotDate;

				if (firstDate == null)
				{
					continue;
				}

				// Only emit points from startDate onwards; forward-fill uses history before startDate.
				var rangeStart = firstDate.Value > startDate ? firstDate.Value : startDate;

				int balanceIdx = 0;
				int snapshotIdx = 0;
				double lastBalance = 0;
				double lastValue = 0;
				double lastInvested = 0;

				// Advance index pointers to the last record strictly before rangeStart so we
				// start with a correctly forward-filled value on rangeStart itself.
				if (accountBalances != null)
				{
					while (balanceIdx < accountBalances.Count && accountBalances[balanceIdx].Date < rangeStart)
					{
						lastBalance = accountBalances[balanceIdx].Value;
						balanceIdx++;
					}
				}

				if (accountSnapshots != null)
				{
					while (snapshotIdx < accountSnapshots.Count && accountSnapshots[snapshotIdx].Date < rangeStart)
					{
						lastValue = accountSnapshots[snapshotIdx].Value;
						lastInvested = accountSnapshots[snapshotIdx].Invested;
						snapshotIdx++;
					}
				}

				for (var date = rangeStart; date <= endDate; date = date.AddDays(1))
				{
					// Apply any records that fall on this exact date.
					if (accountBalances != null)
					{
						while (balanceIdx < accountBalances.Count && accountBalances[balanceIdx].Date == date)
						{
							lastBalance = accountBalances[balanceIdx].Value;
							balanceIdx++;
						}
					}

					if (accountSnapshots != null)
					{
						while (snapshotIdx < accountSnapshots.Count && accountSnapshots[snapshotIdx].Date == date)
						{
							lastValue = accountSnapshots[snapshotIdx].Value;
							lastInvested = accountSnapshots[snapshotIdx].Invested;
							snapshotIdx++;
						}
					}

					result.Add(new AccountValueHistoryPoint
					{
						Date = date,
						AccountId = accountId,
						TotalAssetValue = new Money(currency, (decimal)lastValue),
						TotalInvested = new Money(currency, (decimal)lastInvested),
						CashBalance = new Money(currency, (decimal)lastBalance),
						TotalValue = new Money(currency, (decimal)(lastValue + lastBalance))
					});
				}
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
			if (taxReportCacheService.IsValid)
				return taxReportCacheService.CachedResult!;

			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			// Parallelise the two year-discovery queries — they are fully independent
			var snapshotYearsTask = databaseContext.CalculatedSnapshots
				.Select(s => s.Date.Year)
				.Distinct()
				.ToListAsync(cancellationToken);

			var balanceYearsTask = databaseContext.Balances
				.Select(b => b.Date.Year)
				.Distinct()
				.ToListAsync(cancellationToken);

			await Task.WhenAll(snapshotYearsTask, balanceYearsTask);

			var years = snapshotYearsTask.Result
				.Union(balanceYearsTask.Result)
				.Distinct()
				.OrderBy(y => y)
				.ToList();

			if (years.Count == 0)
				return [];

			var today = DateOnly.FromDateTime(DateTime.Today);
			var targetDates = years
				.SelectMany(y => new[]
				{
					new DateOnly(y, 1, 1),
					y == today.Year ? today : new DateOnly(y, 12, 31)
				})
				.Distinct()
				.OrderBy(d => d)
				.ToList();

			var maxTargetDate = targetDates[^1];

			// Parallelise the three data-load queries — they are fully independent
			var accountsTask = databaseContext.Accounts
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			// Group in SQL so only (Date, AccountId) → sum rows cross the wire, not raw holding rows
			var snapshotsTask = databaseContext.CalculatedSnapshots
				.Where(s => s.Date <= maxTargetDate && s.Holding != null && s.Holding.SymbolProfiles.Any())
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new { g.Key.Date, g.Key.AccountId, TotalValue = g.Sum(x => x.TotalValue) })
				.ToListAsync(cancellationToken);

			var balancesTask = databaseContext.Balances
				.Where(b => b.Date <= maxTargetDate)
				.GroupBy(b => new { b.Date, b.AccountId })
				.Select(g => new { g.Key.Date, AccountId = g.Key.AccountId, Amount = g.Min(x => x.Money.Amount) })
				.ToListAsync(cancellationToken);

			await Task.WhenAll(accountsTask, snapshotsTask, balancesTask);

			var accountById = accountsTask.Result.ToDictionary(a => a.Id, a => a.Name);

			// Pre-index: AccountId → list of (Date, Value) sorted descending by date.
			// This means each per-target-date lookup is O(accounts) with a simple FirstOrDefault
			// instead of re-scanning all rows for every target date.
			var snapshotsByAccount = snapshotsTask.Result
				.GroupBy(s => s.AccountId)
				.ToDictionary(
					g => g.Key,
					g => g.OrderByDescending(s => s.Date).ToList());

			var balancesByAccount = balancesTask.Result
				.GroupBy(b => b.AccountId)
				.ToDictionary(
					g => g.Key,
					g => g.OrderByDescending(b => b.Date).ToList());

			var allAccountIds = snapshotsByAccount.Keys.Union(balancesByAccount.Keys).ToHashSet();
			var currency = serverConfigurationService.PrimaryCurrency;
			var result = new List<TaxReportRow>(targetDates.Count * allAccountIds.Count);

			foreach (var targetDate in targetDates)
			{
				foreach (var accountId in allAccountIds)
				{
					// O(1) dictionary lookup + O(k) scan of one account's dates (k is usually small)
					var snapshotEntry = snapshotsByAccount.TryGetValue(accountId, out var snaps)
						? snaps.FirstOrDefault(s => s.Date <= targetDate)
						: null;

					var balanceEntry = balancesByAccount.TryGetValue(accountId, out var bals)
						? bals.FirstOrDefault(b => b.Date <= targetDate)
						: null;

					if (snapshotEntry == null && balanceEntry == null)
						continue;

					var assetValue = snapshotEntry?.TotalValue ?? 0m;
					var cashBalance = balanceEntry?.Amount ?? 0m;

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

			var ordered = result.OrderBy(r => r.Year).ThenBy(r => r.Date).ThenBy(r => r.AccountName).ToList();
			taxReportCacheService.Store(ordered);
			return ordered;
		}
	}
}
