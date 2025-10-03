using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class TransactionService(
			DatabaseContext databaseContext
		) : ITransactionService
	{
		public async Task<PaginatedTransactionResult> GetTransactionsPaginatedAsync(
			Currency targetCurrency,
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			string transactionType,
			string searchText,
			string sortColumn,
			bool sortAscending,
			int pageNumber,
			int pageSize,
			CancellationToken cancellationToken = default)
		{
			var baseQuery = BuildBaseQuery(startDate, endDate, accountId, symbol, transactionType, searchText);

			// Get total count for pagination
			var totalCount = await baseQuery.CountAsync(cancellationToken);

			// Apply sorting
			var sortedQuery = ApplySorting(baseQuery, sortColumn, sortAscending);

			// Apply pagination and get activities
			var skip = (pageNumber - 1) * pageSize;
			var activities = await sortedQuery
				.Skip(skip)
				.Take(pageSize)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			// Transform to TransactionDisplayModel with client-side evaluation
			var transactions = activities.Select(activity => new TransactionDisplayModel
			{
				Id = activity.Id,
				Date = activity.Date,
				Type = activity.GetType().Name.Replace("Proxy", "").Replace("Activity", ""),
				Symbol = activity.Holding?.SymbolProfiles?.FirstOrDefault()?.Symbol ?? "",
				Name = activity.Holding?.SymbolProfiles?.FirstOrDefault()?.Name ?? "",
				Description = activity.Description ?? "",
				TransactionId = activity.TransactionId ?? "",
				AccountName = activity.Account.Name ?? "",
				Quantity = activity is ActivityWithQuantityAndUnitPrice quantityActivity ? quantityActivity.Quantity : (decimal?)null,
				UnitPrice = activity is ActivityWithQuantityAndUnitPrice quantityActivity2 ? quantityActivity2.UnitPrice : null,
				Currency = activity is ActivityWithQuantityAndUnitPrice quantityActivity3 ? quantityActivity3.UnitPrice.Currency.Symbol : "",
				Amount = activity is ActivityWithAmount amountActivity ? amountActivity.Amount : null,
				TotalValue = activity is ActivityWithQuantityAndUnitPrice quantityActivity4 ? quantityActivity4.TotalTransactionAmount :
							activity is ActivityWithAmount amountActivity2 ? amountActivity2.Amount : null,
				// Calculate fees based on activity type
				Fee = GetFeeForActivity(activity),
				// Calculate taxes based on activity type  
				Tax = GetTaxForActivity(activity),
			}).ToList();

			// Get summary statistics efficiently using separate simpler queries
			var typeBreakdown = await GetTransactionTypeBreakdownAsync(baseQuery, cancellationToken);
			var accountBreakdown = await GetAccountBreakdownAsync(baseQuery, cancellationToken);

			return new PaginatedTransactionResult
			{
				Transactions = transactions,
				TotalCount = totalCount,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TransactionTypeBreakdown = typeBreakdown,
				AccountBreakdown = accountBreakdown
			};
		}

		private static Money? GetFeeForActivity(Activity activity)
		{
			return activity switch
			{
				BuyActivity buy when buy.Fees.Count != 0 => Money.Sum(buy.Fees.Select(f => f.Money)),
				SellActivity sell when sell.Fees.Count != 0 => Money.Sum(sell.Fees.Select(f => f.Money)),
				DividendActivity dividend when dividend.Fees.Count != 0 => Money.Sum(dividend.Fees.Select(f => f.Money)),
				ReceiveActivity receive when receive.Fees.Count != 0 => Money.Sum(receive.Fees.Select(f => f.Money)),
				SendActivity send when send.Fees.Count != 0 => Money.Sum(send.Fees.Select(f => f.Money)),
				_ => null
			};
		}

		private static Money? GetTaxForActivity(Activity activity)
		{
			return activity switch
			{
				BuyActivity buy when buy.Taxes.Count != 0 => Money.Sum(buy.Taxes.Select(t => t.Money)),
				SellActivity sell when sell.Taxes.Count != 0 => Money.Sum(sell.Taxes.Select(t => t.Money)),
				DividendActivity dividend when dividend.Taxes.Count != 0 => Money.Sum(dividend.Taxes.Select(t => t.Money)),
				_ => null
			};
		}
		private static async Task<Dictionary<string, int>> GetTransactionTypeBreakdownAsync(IQueryable<Activity> baseQuery, CancellationToken cancellationToken)
		{
			// Use specific activity types to avoid GetType().Name in LINQ
			var breakdowns = new Dictionary<string, int>();

			// Query for each activity type separately with user-friendly names
			var buyCount = await baseQuery.OfType<BuyActivity>().CountAsync(cancellationToken);
			if (buyCount > 0) breakdowns["Buy"] = buyCount;

			var sellCount = await baseQuery.OfType<SellActivity>().CountAsync(cancellationToken);
			if (sellCount > 0) breakdowns["Sell"] = sellCount;

			var dividendCount = await baseQuery.OfType<DividendActivity>().CountAsync(cancellationToken);
			if (dividendCount > 0) breakdowns["Dividend"] = dividendCount;

			var depositCount = await baseQuery.OfType<CashDepositActivity>().CountAsync(cancellationToken);
			if (depositCount > 0) breakdowns["Deposit"] = depositCount;

			var withdrawalCount = await baseQuery.OfType<CashWithdrawalActivity>().CountAsync(cancellationToken);
			if (withdrawalCount > 0) breakdowns["Withdrawal"] = withdrawalCount;

			var feeCount = await baseQuery.OfType<FeeActivity>().CountAsync(cancellationToken);
			if (feeCount > 0) breakdowns["Fee"] = feeCount;

			var interestCount = await baseQuery.OfType<InterestActivity>().CountAsync(cancellationToken);
			if (interestCount > 0) breakdowns["Interest"] = interestCount;

			var receiveCount = await baseQuery.OfType<ReceiveActivity>().CountAsync(cancellationToken);
			if (receiveCount > 0) breakdowns["Receive"] = receiveCount;

			var sendCount = await baseQuery.OfType<SendActivity>().CountAsync(cancellationToken);
			if (sendCount > 0) breakdowns["Send"] = sendCount;

			var stakingRewardCount = await baseQuery.OfType<StakingRewardActivity>().CountAsync(cancellationToken);
			if (stakingRewardCount > 0) breakdowns["Staking Reward"] = stakingRewardCount;

			var giftFiatCount = await baseQuery.OfType<GiftFiatActivity>().CountAsync(cancellationToken);
			if (giftFiatCount > 0) breakdowns["Gift Fiat"] = giftFiatCount;

			var giftAssetCount = await baseQuery.OfType<GiftAssetActivity>().CountAsync(cancellationToken);
			if (giftAssetCount > 0) breakdowns["Gift Asset"] = giftAssetCount;

			var valuableCount = await baseQuery.OfType<ValuableActivity>().CountAsync(cancellationToken);
			if (valuableCount > 0) breakdowns["Valuable"] = valuableCount;

			var liabilityCount = await baseQuery.OfType<LiabilityActivity>().CountAsync(cancellationToken);
			if (liabilityCount > 0) breakdowns["Liability"] = liabilityCount;

			var repayBondCount = await baseQuery.OfType<RepayBondActivity>().CountAsync(cancellationToken);
			if (repayBondCount > 0) breakdowns["Repay Bond"] = repayBondCount;

			return breakdowns;
		}

		private static async Task<Dictionary<string, int>> GetAccountBreakdownAsync(IQueryable<Activity> baseQuery, CancellationToken cancellationToken)
		{
			return await baseQuery
				.GroupBy(a => a.Account.Name)
				.Select(g => new { AccountName = g.Key ?? "", Count = g.Count() })
				.ToDictionaryAsync(x => x.AccountName, x => x.Count, cancellationToken);
		}

		public async Task<int> GetTransactionCountAsync(
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			string transactionType,
			string searchText,
			CancellationToken cancellationToken = default)
		{
			var baseQuery = BuildBaseQuery(startDate, endDate, accountId, symbol, transactionType, searchText);
			return await baseQuery.CountAsync(cancellationToken);
		}

		private IQueryable<Activity> BuildBaseQuery(
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			string transactionType,
			string searchText)
		{
			var query = databaseContext.Activities
				.Include(a => a.Account)
				.Include(a => a.Holding)
				.Where(a => a.Date >= startDate.ToDateTime(TimeOnly.MinValue) && a.Date <= endDate.ToDateTime(TimeOnly.MinValue))
				.Where(x => !(x is KnownBalanceActivity)); // Exclude KnownBalanceActivity entries

			if (accountId > 0)
			{
				query = query.Where(a => a.Account.Id == accountId);
			}

			if (!string.IsNullOrWhiteSpace(symbol))
			{
				query = query.Where(a => a.Holding != null && a.Holding.SymbolProfiles.Any(x => x.Symbol == symbol));
			}

			if (!string.IsNullOrWhiteSpace(transactionType))
			{
				// Use specific type checks instead of GetType().Name.Replace() which can't be translated to SQL
				// Handle both user-friendly names and database type names
				query = transactionType switch
				{
					"Buy" => query.Where(a => a is BuyActivity),
					"Sell" => query.Where(a => a is SellActivity),
					"Dividend" => query.Where(a => a is DividendActivity),
					"Deposit" or "CashDeposit" => query.Where(a => a is CashDepositActivity),
					"Withdrawal" or "CashWithdrawal" => query.Where(a => a is CashWithdrawalActivity),
					"Fee" => query.Where(a => a is FeeActivity),
					"Interest" => query.Where(a => a is InterestActivity),
					"Receive" => query.Where(a => a is ReceiveActivity),
					"Send" => query.Where(a => a is SendActivity),
					"Staking Reward" or "StakingReward" => query.Where(a => a is StakingRewardActivity),
					"Gift Fiat" or "GiftFiat" => query.Where(a => a is GiftFiatActivity),
					"Gift Asset" or "GiftAsset" => query.Where(a => a is GiftAssetActivity),
					"Valuable" => query.Where(a => a is ValuableActivity),
					"Liability" => query.Where(a => a is LiabilityActivity),
					"Repay Bond" or "RepayBond" => query.Where(a => a is RepayBondActivity),
					_ => query // If type not recognized, return all activities
				};
			}

			if (!string.IsNullOrWhiteSpace(searchText))
			{
				var searchLower = searchText.ToLower();
				query = query.Where(a =>
					(a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Symbol.ToLower().Contains(searchLower))) ||
					(a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Name.ToLower().Contains(searchLower))) ||
					a.Description.ToLower().Contains(searchLower) ||
					a.TransactionId.ToLower().Contains(searchLower));
			}

			return query;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "Expression")]
		private static IQueryable<Activity> ApplySorting(IQueryable<Activity> query, string sortColumn, bool sortAscending)
		{
			Expression<Func<Activity, object>> sortExpression = sortColumn switch
			{
				"Date" => a => a.Date,
				"Type" => a => a.GetType().Name,
				"Symbol" => a => a.Holding != null && a.Holding.SymbolProfiles != null ? a.Holding.SymbolProfiles[0].Symbol : "",
				"Name" => a => a.Holding != null && a.Holding.SymbolProfiles != null ? a.Holding.SymbolProfiles[0].Name ?? "" : "",
				"AccountName" => a => a.Account.Name,
				"TotalValue" => a => a is ActivityWithQuantityAndUnitPrice ? ((ActivityWithQuantityAndUnitPrice)a).TotalTransactionAmount.Amount : 
								   a is ActivityWithAmount ? ((ActivityWithAmount)a).Amount.Amount : 0,
				"Description" => a => a.Description ?? "",
				_ => a => a.Date // Default sort
			};

			return sortAscending ? query.OrderBy(sortExpression) : query.OrderByDescending(sortExpression);
		}

		public async Task<List<string>> GetTransactionTypesAsync(CancellationToken cancellationToken = default)
		{
			// Return user-friendly transaction type names that match the breakdown
			var existingTypes = new List<string>();

			// Check each type and add user-friendly name if it exists
			if (await databaseContext.Activities.OfType<BuyActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Buy");

			if (await databaseContext.Activities.OfType<SellActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Sell");

			if (await databaseContext.Activities.OfType<DividendActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Dividend");

			if (await databaseContext.Activities.OfType<CashDepositActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Deposit");

			if (await databaseContext.Activities.OfType<CashWithdrawalActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Withdrawal");

			if (await databaseContext.Activities.OfType<FeeActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Fee");

			if (await databaseContext.Activities.OfType<InterestActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Interest");

			if (await databaseContext.Activities.OfType<ReceiveActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Receive");

			if (await databaseContext.Activities.OfType<SendActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Send");

			if (await databaseContext.Activities.OfType<StakingRewardActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Staking Reward");

			if (await databaseContext.Activities.OfType<GiftFiatActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Gift Fiat");

			if (await databaseContext.Activities.OfType<GiftAssetActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Gift Asset");

			if (await databaseContext.Activities.OfType<ValuableActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Valuable");

			if (await databaseContext.Activities.OfType<LiabilityActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Liability");

			if (await databaseContext.Activities.OfType<RepayBondActivity>().AnyAsync(cancellationToken))
				existingTypes.Add("Repay Bond");

			return existingTypes.OrderBy(x => x).ToList();
		}
	}
}
