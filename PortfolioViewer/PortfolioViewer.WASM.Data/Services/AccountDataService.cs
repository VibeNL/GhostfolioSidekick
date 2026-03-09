using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
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

		public async Task<List<TaxAccountDisplayModel>> GetTaxAccountDetailsAsync(int year)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync();
			var startDate = new DateTime(year, 1, 1);
			var endDate = new DateTime(year, 12, 31);
			var accounts = await databaseContext.Accounts
				.Include(a => a.Platform)
				.Include(a => a.Activities)
				.ThenInclude(act => act.Holding)
				.ThenInclude(h => h!.SymbolProfiles)
				.AsNoTracking()
				.ToListAsync();

			var result = new List<TaxAccountDisplayModel>();

			foreach (var account in accounts)
			{
				var model = new TaxAccountDisplayModel
				{
					Name = account.Name,
					AccountType = account.Platform?.Name ?? "Unknown"
				};

				// Holdings (as of end of year)
                var holdings = account.Activities
                    .Where(a => a.Holding != null && a.Holding.SymbolProfiles != null && a.Date <= endDate)
                    .GroupBy(a => a.Holding != null ? a.Holding.Id : 0)
					.Select(g =>
					{
						var lastActivity = g.OrderByDescending(a => a.Date).FirstOrDefault();
						return new TaxHoldingDisplayModel
						{
							Symbol = lastActivity?.Holding?.SymbolProfiles?.FirstOrDefault()?.Symbol ?? "",
							Quantity = lastActivity is ActivityWithQuantityAndUnitPrice q ? q.Quantity : 0,
							Value = lastActivity is ActivityWithQuantityAndUnitPrice q2 ? q2.Quantity * (q2.UnitPrice?.Amount ?? 0) : 0,
							AcquisitionDate = lastActivity?.Date ?? DateTime.MinValue
						};
					})
					.ToList();
				model.Holdings = holdings;

				// Transactions
				model.Transactions = account.Activities
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.Select(a => new TaxTransactionDisplayModel
					{
						Date = a.Date,
						Type = a.GetType().Name.Replace("Proxy", "").Replace("Activity", ""),
						Symbol = a.Holding?.SymbolProfiles?.FirstOrDefault()?.Symbol ?? "",
						Amount = a is ActivityWithAmount aa ? aa.Amount.Amount : 0,
                        Fees = a is BuyActivity ba ? (ba.Fees?.Sum(f => f.Money.Amount) ?? 0) :
                            (a is SellActivity sa ? (sa.Fees?.Sum(f => f.Money.Amount) ?? 0) :
                            (a is DividendActivity da ? (da.Fees?.Sum(f => f.Money.Amount) ?? 0) : 0)),
						Notes = a.Description ?? ""
					})
					.ToList();

				// Dividends
				model.Dividends = account.Activities
					.Where(a => a is DividendActivity && a.Date >= startDate && a.Date <= endDate)
					.Select(a =>
					{
						var dividend = a as DividendActivity;
						return new TaxDividendDisplayModel
						{
							Date = dividend?.Date ?? DateTime.MinValue,
							Symbol = dividend?.Holding?.SymbolProfiles?.FirstOrDefault()?.Symbol ?? "",
							Amount = dividend?.Amount.Amount ?? 0,
							TaxWithheld = dividend?.Taxes.Sum(t => t.Money.Amount) ?? 0
						};
					})
					.ToList();

				// Realized Gains/Losses
				model.RealizedGainsLosses = account.Activities
					.Where(a => a is SellActivity && a.Date >= startDate && a.Date <= endDate)
					.Select(a =>
					{
						var sell = a as SellActivity;
						var holdingSymbol = sell?.Holding != null && sell.Holding.SymbolProfiles != null ? sell.Holding.SymbolProfiles.FirstOrDefault()?.Symbol ?? "" : "";
						var amount = a is ActivityWithQuantityAndUnitPrice aq ? aq.TotalTransactionAmount?.Amount ?? 0 : 0;
						return new TaxGainLossDisplayModel
						{
							Symbol = holdingSymbol,
							Amount = amount,
							Date = sell?.Date ?? DateTime.MinValue
						};
					})
					.ToList();

				result.Add(model);
			}

			return result;
		}
	}
}
