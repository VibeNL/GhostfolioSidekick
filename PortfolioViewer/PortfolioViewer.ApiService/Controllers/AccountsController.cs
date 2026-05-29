using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AccountsController(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IApplicationSettings applicationSettings) : ControllerBase
	{
		private string PrimaryCurrencySymbol =>
			applicationSettings.ConfigurationInstance?.Settings?.PrimaryCurrency ?? "EUR";

		[HttpGet]
		public async Task<IActionResult> GetAccounts([FromQuery] string? symbolFilter, CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var query = db.Accounts
				.Include(a => a.Platform)
				.AsNoTracking()
				.OrderBy(a => a.Name)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(symbolFilter))
			{
				query = query.Where(a => a.Activities.Any(h => h.Holding != null && h.Holding.SymbolProfiles.Any(s => s.Symbol == symbolFilter)));
			}

			var accounts = await query.ToListAsync(cancellationToken);
			return Ok(accounts.Select(MapAccount));
		}

		[HttpGet("{accountId:int}")]
		public async Task<IActionResult> GetAccountById(int accountId, CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var account = await db.Accounts
				.Include(a => a.Platform)
				.AsNoTracking()
				.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

			if (account == null)
			{
				return NotFound();
			}

			return Ok(MapAccount(account));
		}

		[HttpGet("min-date")]
		public async Task<IActionResult> GetMinDate(CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var minDate = await db.Activities.MinAsync(s => s.Date, cancellationToken);
			return Ok(DateOnly.FromDateTime(minDate).ToString("yyyy-MM-dd"));
		}

		[HttpGet("symbol-profiles")]
		public async Task<IActionResult> GetSymbolProfiles([FromQuery] int? accountFilter, CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			List<string> symbols;

			if (!accountFilter.HasValue)
			{
				symbols = await db.SymbolProfiles
					.OrderBy(s => s.Symbol)
					.Select(s => s.Symbol)
					.ToListAsync(cancellationToken);
			}
			else
			{
				symbols = await db.Holdings
					.Where(x => x.Activities.Any(y => y.Account.Id == accountFilter))
					.SelectMany(x => x.SymbolProfiles)
					.OrderBy(s => s.Symbol)
					.Select(s => s.Symbol)
					.ToListAsync(cancellationToken);
			}

			return Ok(symbols);
		}

		[HttpGet("value-history")]
		public async Task<IActionResult> GetAccountValueHistory(
			[FromQuery] DateOnly startDate,
			[FromQuery] DateOnly endDate,
			CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			var snapShots = await db.CalculatedSnapshots
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

			var allBalances = await db.Balances
				.Where(s => s.Date <= endDate)
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new
				{
					g.Key.Date,
					Value = g.Min(x => (double)x.Money.Amount),
					g.Key.AccountId,
				})
				.ToListAsync(cancellationToken);

			var balancesByAccount = allBalances
				.GroupBy(b => b.AccountId)
				.ToDictionary(g => g.Key, g => g.OrderBy(b => b.Date).ToList());

			var snapshotsByAccount = snapShots
				.GroupBy(s => s.AccountId)
				.ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).ToList());

			var allAccountIds = balancesByAccount.Keys.Union(snapshotsByAccount.Keys).Distinct();

			var result = new List<AccountValueHistoryPointDto>();

			foreach (var accountId in allAccountIds)
			{
				balancesByAccount.TryGetValue(accountId, out var accountBalances);
				snapshotsByAccount.TryGetValue(accountId, out var accountSnapshots);

				var firstBalanceDate = accountBalances?.FirstOrDefault()?.Date;
				var firstSnapshotDate = accountSnapshots?.FirstOrDefault()?.Date;

				DateOnly? firstDate;
				if (firstBalanceDate.HasValue && firstSnapshotDate.HasValue)
				{
					firstDate = firstBalanceDate.Value < firstSnapshotDate.Value ? firstBalanceDate.Value : firstSnapshotDate.Value;
				}
				else
				{
					firstDate = firstBalanceDate ?? firstSnapshotDate;
				}

				if (firstDate == null)
				{
					continue;
				}

				var rangeStart = firstDate.Value > startDate ? firstDate.Value : startDate;

				int balanceIdx = 0;
				int snapshotIdx = 0;
				double lastBalance = 0;
				double lastValue = 0;
				double lastInvested = 0;

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

					result.Add(new AccountValueHistoryPointDto
					{
						Date = date,
						AccountId = accountId,
						TotalAssetValue = (decimal)lastValue,
						TotalInvested = (decimal)lastInvested,
						CashBalance = (decimal)lastBalance,
						TotalValue = (decimal)(lastValue + lastBalance),
						Currency = PrimaryCurrencySymbol
					});
				}
			}

			return Ok(result.OrderBy(x => x.Date).ThenBy(x => x.AccountId));
		}

		[HttpGet("tax-report")]
		public async Task<IActionResult> GetTaxReport(CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			var snapshotYearsTask = db.CalculatedSnapshots
				.Select(s => s.Date.Year).Distinct().ToListAsync(cancellationToken);
			var balanceYearsTask = db.Balances
				.Select(b => b.Date.Year).Distinct().ToListAsync(cancellationToken);

			await Task.WhenAll(snapshotYearsTask, balanceYearsTask);

			var years = snapshotYearsTask.Result.Union(balanceYearsTask.Result).Distinct().OrderBy(y => y).ToList();
			if (years.Count == 0)
			{
				return Ok(Array.Empty<TaxReportRowDto>());
			}

			var today = DateOnly.FromDateTime(DateTime.Today);
			var targetDates = years
				.SelectMany(y => new[] { new DateOnly(y, 1, 1), y == today.Year ? today : new DateOnly(y, 12, 31) })
				.Distinct()
				.OrderBy(d => d)
				.ToList();

			var maxTargetDate = targetDates[^1];

			var accountsTask = db.Accounts.AsNoTracking().ToListAsync(cancellationToken);
			var snapshotsTask = db.CalculatedSnapshots
				.Where(s => s.Date <= maxTargetDate && s.Holding != null && s.Holding.SymbolProfiles.Any())
				.GroupBy(s => new { s.Date, s.AccountId })
				.Select(g => new { g.Key.Date, g.Key.AccountId, TotalValue = g.Sum(x => x.TotalValue) })
				.ToListAsync(cancellationToken);
			var balancesTask = db.Balances
				.Where(b => b.Date <= maxTargetDate)
				.GroupBy(b => new { b.Date, b.AccountId })
				.Select(g => new { g.Key.Date, g.Key.AccountId, Amount = g.Min(x => x.Money.Amount) })
				.ToListAsync(cancellationToken);

			await Task.WhenAll(accountsTask, snapshotsTask, balancesTask);

			var accountById = accountsTask.Result.ToDictionary(a => a.Id, a => a.Name);

			var snapshotsByAccount = snapshotsTask.Result
				.GroupBy(s => s.AccountId)
				.ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Date).ToList());

			var balancesByAccount = balancesTask.Result
				.GroupBy(b => b.AccountId)
				.ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.Date).ToList());

			var allAccountIds = snapshotsByAccount.Keys.Union(balancesByAccount.Keys).ToHashSet();
			var currency = PrimaryCurrencySymbol;
			var result = new List<TaxReportRowDto>();

			foreach (var targetDate in targetDates)
			{
				foreach (var accountId in allAccountIds)
				{
					var snapshotEntry = snapshotsByAccount.TryGetValue(accountId, out var snaps) ? snaps.FirstOrDefault(s => s.Date <= targetDate) : null;
					var balanceEntry = balancesByAccount.TryGetValue(accountId, out var bals) ? bals.FirstOrDefault(b => b.Date <= targetDate) : null;

					if (snapshotEntry == null && balanceEntry == null)
					{
						continue;
					}

					var assetValue = snapshotEntry?.TotalValue ?? 0m;
					var cashBalance = balanceEntry?.Amount ?? 0m;

					result.Add(new TaxReportRowDto
					{
						Year = targetDate.Year,
						Date = targetDate,
						AccountId = accountId,
						AccountName = accountById.TryGetValue(accountId, out var name) ? name : $"Account {accountId}",
						AssetValue = assetValue,
						CashBalance = cashBalance,
						TotalValue = assetValue + cashBalance,
						Currency = currency
					});
				}
			}

			return Ok(result.OrderBy(r => r.Year).ThenBy(r => r.Date).ThenBy(r => r.AccountName));
		}

		private static AccountDto MapAccount(Account a) => new()
		{
			Id = a.Id,
			Name = a.Name,
			Comment = a.Comment,
			SyncActivities = a.SyncActivities,
			SyncBalance = a.SyncBalance,
			PlatformName = a.Platform?.Name,
		};

		public class AccountDto
		{
			public int Id { get; set; }
			public string Name { get; set; } = string.Empty;
			public string? Comment { get; set; }
			public bool SyncActivities { get; set; }
			public bool SyncBalance { get; set; }
			public string? PlatformName { get; set; }
		}

		public class AccountValueHistoryPointDto
		{
			public DateOnly Date { get; set; }
			public int AccountId { get; set; }
			public decimal TotalAssetValue { get; set; }
			public decimal TotalInvested { get; set; }
			public decimal CashBalance { get; set; }
			public decimal TotalValue { get; set; }
			public string Currency { get; set; } = "EUR";
		}

		public class TaxReportRowDto
		{
			public int Year { get; set; }
			public DateOnly Date { get; set; }
			public int AccountId { get; set; }
			public string AccountName { get; set; } = string.Empty;
			public decimal AssetValue { get; set; }
			public decimal CashBalance { get; set; }
			public decimal TotalValue { get; set; }
			public string Currency { get; set; } = "EUR";
		}
	}
}
