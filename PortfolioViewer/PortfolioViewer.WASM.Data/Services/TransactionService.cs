using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class TransactionService(
			IDbContextFactory<DatabaseContext> dbContextFactory
		) : ITransactionService
	{
		public async Task<PaginatedTransactionResult> GetTransactionsPaginatedAsync(
			TransactionQueryParameters parameters,
			CancellationToken cancellationToken = default)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var baseQuery = await BuildBaseQuery(
				databaseContext,
				parameters.StartDate,
				parameters.EndDate,
				parameters.AccountId,
				parameters.Symbol,
				parameters.TransactionTypes,
				parameters.SearchText);

			// Get total count for pagination
			var totalCount = await baseQuery.CountAsync(cancellationToken);

			// Apply sorting
			var sortedQuery = ApplySorting(baseQuery, parameters.SortColumn, parameters.SortAscending);

			// Apply pagination and get activities
			var skip = (parameters.PageNumber - 1) * parameters.PageSize;
			var activities = await sortedQuery
				.Skip(skip)
				.Take(parameters.PageSize)
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
				TotalValue = GetTotalValueForActivity(activity),
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
				PageNumber = parameters.PageNumber,
				PageSize = parameters.PageSize,
				TransactionTypeBreakdown = typeBreakdown,
				AccountBreakdown = accountBreakdown
			};
		}

		private static Money? GetTotalValueForActivity(Activity activity)
		{
			if (activity is ActivityWithQuantityAndUnitPrice quantityActivity)
			{
				return quantityActivity.TotalTransactionAmount;
			}

			if (activity is ActivityWithAmount amountActivity)
			{
				return amountActivity.Amount;
			}

			return null;
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

		private static async Task<IQueryable<Activity>> BuildBaseQuery(
			DatabaseContext databaseContext,
			DateOnly startDate,
			DateOnly endDate,
			int accountId,
			string symbol,
			List<string> transactionTypes,
			string searchText)
		{
			var query = databaseContext.Activities
				.Include(a => a.Account)
				.Include(a => a.Holding)
				.Where(a => a.Date >= startDate.ToDateTime(TimeOnly.MinValue) && a.Date <= endDate.ToDateTime(TimeOnly.MinValue));

			if (accountId > 0)
			{
				query = query.Where(a => a.Account.Id == accountId);
			}

			if (!string.IsNullOrWhiteSpace(symbol))
			{
				query = query.Where(a => a.Holding != null && a.Holding.SymbolProfiles.Any(x => x.Symbol == symbol));
			}

			if (transactionTypes != null && transactionTypes.Count > 0)
			{
				var normalizedTypes = transactionTypes.Select(t => t.Replace("Activity", "").Replace("Proxy", "")).ToList();
				var typePredicates = new List<Expression<Func<Activity, bool>>>();

				foreach (var type in normalizedTypes)
				{
					switch (type)
					{
						case "Buy": typePredicates.Add(a => a is BuyActivity); break;
						case "Sell": typePredicates.Add(a => a is SellActivity); break;
						case "Dividend": typePredicates.Add(a => a is DividendActivity); break;
						case "Deposit": case "CashDeposit": typePredicates.Add(a => a is CashDepositActivity); break;
						case "Withdrawal": case "CashWithdrawal": typePredicates.Add(a => a is CashWithdrawalActivity); break;
						case "Fee": typePredicates.Add(a => a is FeeActivity); break;
						case "Interest": typePredicates.Add(a => a is InterestActivity); break;
						case "Receive": typePredicates.Add(a => a is ReceiveActivity); break;
						case "Send": typePredicates.Add(a => a is SendActivity); break;
						case "Staking Reward": case "StakingReward": typePredicates.Add(a => a is StakingRewardActivity); break;
						case "Gift Fiat": case "GiftFiat": typePredicates.Add(a => a is GiftFiatActivity); break;
						case "Gift Asset": case "GiftAsset": typePredicates.Add(a => a is GiftAssetActivity); break;
						case "Valuable": typePredicates.Add(a => a is ValuableActivity); break;
						case "Liability": typePredicates.Add(a => a is LiabilityActivity); break;
						case "Repay Bond": case "RepayBond": typePredicates.Add(a => a is RepayBondActivity); break;
						case "KnownBalance": case "Known Balance": typePredicates.Add(a => a is KnownBalanceActivity); break;
						default: break;
					}
				}

				if (typePredicates.Count > 0)
				{
					// Combine all predicates using Expression.OrElse
					var param = Expression.Parameter(typeof(Activity), "a");
					Expression? body = null;
					foreach (var predicate in typePredicates)
					{
						var invoked = Expression.Invoke(predicate, param);
						body = body == null ? invoked : Expression.OrElse(body, invoked);
					}
					if (body != null)
					{
						var lambda = Expression.Lambda<Func<Activity, bool>>(body, param);
						query = query.Where(lambda);
					}
				}
			}

			if (!string.IsNullOrWhiteSpace(searchText))
			{
				var searchLower = searchText.ToLower();
				query = query.Where(a =>
					(a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Symbol.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase))) ||
					(a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Name != null && sp.Name.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase))) ||
					(a.Description != null && a.Description.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase)) ||
					a.TransactionId.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase));
			}

			return query;
		}

		private static Expression<Func<Activity, object>> GetSortExpressionForTotalValue()
		{
			return a => a is ActivityWithQuantityAndUnitPrice ? ((ActivityWithQuantityAndUnitPrice)a).TotalTransactionAmount.Amount :
						a is ActivityWithAmount ? ((ActivityWithAmount)a).Amount.Amount : (object)0;
		}

		private static IQueryable<Activity> ApplySorting(IQueryable<Activity> query, string sortColumn, bool sortAscending)
		{
			Expression<Func<Activity, object>> sortExpression = sortColumn switch
			{
				"Date" => a => a.Date,
				"Type" => a => a.GetType().Name,
				"Symbol" => a => a.Holding != null && a.Holding.SymbolProfiles != null ? a.Holding.SymbolProfiles[0].Symbol : "",
				"Name" => a => a.Holding != null && a.Holding.SymbolProfiles != null ? a.Holding.SymbolProfiles[0].Name ?? "" : "",
				"AccountName" => a => a.Account.Name,
				"TotalValue" => GetSortExpressionForTotalValue(),
				"Description" => a => a.Description ?? "",
				_ => a => a.Date // Default sort
			};

			return sortAscending ? query.OrderBy(sortExpression) : query.OrderByDescending(sortExpression);
		}

		public async Task<List<string>> GetTransactionTypesAsync(CancellationToken cancellationToken = default)
		{
			// Get all distinct activity types from the database
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var typeNames = await databaseContext.Activities
				.Select(a => a.GetType().Name)
				.Distinct()
				.ToListAsync(cancellationToken);

			// Remove 'Activity' suffix for readability
			var result = typeNames
				.Select(typeName => typeName.Replace("Activity", "").Replace("Proxy", ""))
				.Distinct()
				.OrderBy(x => x)
				.ToList();

			return result;
		}
	}
}
