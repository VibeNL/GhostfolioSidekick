using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class TransactionsController(IDbContextFactory<DatabaseContext> dbContextFactory) : ControllerBase
	{
		[HttpPost("paginated")]
		public async Task<IActionResult> GetTransactionsPaginated(
			[FromBody] TransactionQueryDto parameters,
			CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var baseQuery = BuildBaseQuery(db, parameters);

			var totalCount = await baseQuery.CountAsync(cancellationToken);
			var sortedQuery = ApplySorting(baseQuery, parameters.SortColumn, parameters.SortAscending);
			var skip = (parameters.PageNumber - 1) * parameters.PageSize;

			var activities = await sortedQuery
				.Skip(skip)
				.Take(parameters.PageSize)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			var transactions = activities.Select(MapActivity).ToList();

			var typeBreakdown = await GetTypeBreakdownAsync(baseQuery, cancellationToken);
			var accountBreakdown = await baseQuery
				.GroupBy(a => a.Account.Name)
				.Select(g => new { AccountName = g.Key ?? "", Count = g.Count() })
				.ToDictionaryAsync(x => x.AccountName, x => x.Count, cancellationToken);

			return Ok(new PaginatedTransactionResultDto
			{
				Transactions = transactions,
				TotalCount = totalCount,
				PageNumber = parameters.PageNumber,
				PageSize = parameters.PageSize,
				TransactionTypeBreakdown = typeBreakdown,
				AccountBreakdown = accountBreakdown
			});
		}

		[HttpGet("types")]
		public async Task<IActionResult> GetTransactionTypes(CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var typeNames = await db.Activities
				.Select(a => a.GetType().Name)
				.Distinct()
				.ToListAsync(cancellationToken);

			var result = typeNames
				.Select(t => t.Replace("Activity", "").Replace("Proxy", ""))
				.Distinct()
				.OrderBy(x => x)
				.ToList();

			return Ok(result);
		}

		private static TransactionDisplayModelDto MapActivity(Activity activity) => new()
		{
			Id = activity.Id,
			Date = activity.Date,
			Type = activity.GetType().Name.Replace("Proxy", "").Replace("Activity", ""),
			Symbol = activity.Holding?.SymbolProfiles?.FirstOrDefault()?.Symbol ?? "",
			Name = activity.Holding?.SymbolProfiles?.FirstOrDefault()?.Name ?? "",
			Description = activity.Description ?? "",
			TransactionId = activity.TransactionId ?? "",
			AccountName = activity.Account.Name ?? "",
			Quantity = activity is ActivityWithQuantityAndUnitPrice qActivity ? qActivity.Quantity : null,
			UnitPriceAmount = activity is ActivityWithQuantityAndUnitPrice qActivity2 ? qActivity2.UnitPrice.Amount : null,
			UnitPriceCurrency = activity is ActivityWithQuantityAndUnitPrice qActivity3 ? qActivity3.UnitPrice.Currency.Symbol : "",
			AmountValue = activity is ActivityWithAmount aActivity ? aActivity.Amount?.Amount : null,
			AmountCurrency = activity is ActivityWithAmount aActivity2 ? aActivity2.Amount?.Currency.Symbol ?? "" : "",
			TotalValueAmount = GetTotalValueAmount(activity),
			TotalValueCurrency = GetTotalValueCurrency(activity),
			FeeAmount = GetFeeAmount(activity),
			FeeCurrency = GetFeeCurrency(activity),
			TaxAmount = GetTaxAmount(activity),
			TaxCurrency = GetTaxCurrency(activity),
		};

		private static decimal? GetTotalValueAmount(Activity activity)
		{
			if (activity is ActivityWithQuantityAndUnitPrice q)
			{
				return q.UnitPrice.Times(q.Quantity)?.Amount;
			}

			return activity is ActivityWithAmount a ? a.Amount?.Amount : null;
		}

		private static string GetTotalValueCurrency(Activity activity)
		{
			if (activity is ActivityWithQuantityAndUnitPrice q)
			{
				return q.UnitPrice.Currency.Symbol;
			}

			return activity is ActivityWithAmount a ? a.Amount?.Currency.Symbol ?? "" : "";
		}

		private static decimal? GetFeeAmount(Activity activity) => activity switch
		{
			BuyActivity buy when buy.Fees.Count != 0 => Money.Sum(buy.Fees)?.Amount,
			SellActivity sell when sell.Fees.Count != 0 => Money.Sum(sell.Fees)?.Amount,
			DividendActivity div when div.Fees.Count != 0 => Money.Sum(div.Fees)?.Amount,
			ReceiveActivity recv when recv.Fees.Count != 0 => Money.Sum(recv.Fees)?.Amount,
			SendActivity send when send.Fees.Count != 0 => Money.Sum(send.Fees)?.Amount,
			_ => null
		};

		private static string GetFeeCurrency(Activity activity) => activity switch
		{
			BuyActivity buy when buy.Fees.Count != 0 => Money.Sum(buy.Fees)?.Currency.Symbol ?? "",
			SellActivity sell when sell.Fees.Count != 0 => Money.Sum(sell.Fees)?.Currency.Symbol ?? "",
			DividendActivity div when div.Fees.Count != 0 => Money.Sum(div.Fees)?.Currency.Symbol ?? "",
			ReceiveActivity recv when recv.Fees.Count != 0 => Money.Sum(recv.Fees)?.Currency.Symbol ?? "",
			SendActivity send when send.Fees.Count != 0 => Money.Sum(send.Fees)?.Currency.Symbol ?? "",
			_ => ""
		};

		private static decimal? GetTaxAmount(Activity activity) => activity switch
		{
			BuyActivity buy when buy.Taxes.Count != 0 => Money.Sum(buy.Taxes)?.Amount,
			SellActivity sell when sell.Taxes.Count != 0 => Money.Sum(sell.Taxes)?.Amount,
			DividendActivity div when div.Taxes.Count != 0 => Money.Sum(div.Taxes)?.Amount,
			_ => null
		};

		private static string GetTaxCurrency(Activity activity) => activity switch
		{
			BuyActivity buy when buy.Taxes.Count != 0 => Money.Sum(buy.Taxes)?.Currency.Symbol ?? "",
			SellActivity sell when sell.Taxes.Count != 0 => Money.Sum(sell.Taxes)?.Currency.Symbol ?? "",
			DividendActivity div when div.Taxes.Count != 0 => Money.Sum(div.Taxes)?.Currency.Symbol ?? "",
			_ => ""
		};

		private static IQueryable<Activity> BuildBaseQuery(DatabaseContext db, TransactionQueryDto p)
		{
			IQueryable<Activity> query = db.Activities
				.Include(a => a.Account)
				.Include(a => a.Holding)
				.Where(a => a.Date >= p.StartDate.ToDateTime(TimeOnly.MinValue) && a.Date <= p.EndDate.ToDateTime(TimeOnly.MinValue));

			if (p.AccountId > 0)
			{
				query = query.Where(a => a.Account.Id == p.AccountId);
			}

			if (!string.IsNullOrWhiteSpace(p.Symbol))
			{
				query = query.Where(a => a.Holding != null && a.Holding.SymbolProfiles.Any(x => x.Symbol == p.Symbol));
			}

			if (p.TransactionTypes != null && p.TransactionTypes.Count > 0)
			{
				var normalizedTypes = p.TransactionTypes.Select(t => t.Replace("Activity", "").Replace("Proxy", "")).ToList();
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
						case "Correction": typePredicates.Add(a => a is CorrectionActivity); break;
						default: break;
					}
				}

				if (typePredicates.Count > 0)
				{
					ParameterExpression param = Expression.Parameter(typeof(Activity), "a");
					Expression? body = null;
					foreach (var predicate in typePredicates)
					{
						var invoked = Expression.Invoke(predicate, param);
						body = body == null ? invoked : Expression.OrElse(body, invoked);
					}

					if (body != null)
					{
						query = query.Where(Expression.Lambda<Func<Activity, bool>>(body, param));
					}
				}
			}

			if (!string.IsNullOrWhiteSpace(p.SearchText))
			{
				var searchLower = p.SearchText.ToLower();
				query = query.Where(a =>
					(a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Symbol.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase))) ||
					(a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Name != null && sp.Name.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase))) ||
					(a.Description != null && a.Description.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase)) ||
					a.TransactionId.Contains(searchLower, StringComparison.CurrentCultureIgnoreCase));
			}

			return query;
		}

		private static IQueryable<Activity> ApplySorting(IQueryable<Activity> query, string sortColumn, bool sortAscending)
		{
			if (sortColumn == "TotalValue")
			{
				// Pattern matching not supported in EF expression trees – sort by a computed scalar helper.
				// Fall through to default date sort to avoid an expression-tree exception.
				return sortAscending ? query.OrderBy(a => a.Date) : query.OrderByDescending(a => a.Date);
			}

			Expression<Func<Activity, object>> sortExpr = sortColumn switch
			{
				"Date" => a => a.Date,
				"Type" => a => a.GetType().Name,
				"Symbol" => a => a.Holding != null && a.Holding.SymbolProfiles.Count > 0 ? a.Holding.SymbolProfiles[0].Symbol : "",
				"Name" => a => a.Holding != null && a.Holding.SymbolProfiles.Count > 0 ? (a.Holding.SymbolProfiles[0].Name ?? "") : "",
				"AccountName" => a => a.Account.Name,
				"Description" => a => a.Description ?? "",
				_ => a => a.Date
			};

			return sortAscending ? query.OrderBy(sortExpr) : query.OrderByDescending(sortExpr);
		}

		private static async Task<Dictionary<string, int>> GetTypeBreakdownAsync(IQueryable<Activity> q, CancellationToken ct)
		{
			var result = new Dictionary<string, int>();
			void Add(string key, int count) { if (count > 0) { result[key] = count; } }

			Add("Buy", await q.OfType<BuyActivity>().CountAsync(ct));
			Add("Sell", await q.OfType<SellActivity>().CountAsync(ct));
			Add("Dividend", await q.OfType<DividendActivity>().CountAsync(ct));
			Add("Deposit", await q.OfType<CashDepositActivity>().CountAsync(ct));
			Add("Withdrawal", await q.OfType<CashWithdrawalActivity>().CountAsync(ct));
			Add("Fee", await q.OfType<FeeActivity>().CountAsync(ct));
			Add("Interest", await q.OfType<InterestActivity>().CountAsync(ct));
			Add("Receive", await q.OfType<ReceiveActivity>().CountAsync(ct));
			Add("Send", await q.OfType<SendActivity>().CountAsync(ct));
			Add("Staking Reward", await q.OfType<StakingRewardActivity>().CountAsync(ct));
			Add("Gift Fiat", await q.OfType<GiftFiatActivity>().CountAsync(ct));
			Add("Gift Asset", await q.OfType<GiftAssetActivity>().CountAsync(ct));
			Add("Valuable", await q.OfType<ValuableActivity>().CountAsync(ct));
			Add("Liability", await q.OfType<LiabilityActivity>().CountAsync(ct));
			Add("Repay Bond", await q.OfType<RepayBondActivity>().CountAsync(ct));
			Add("Correction", await q.OfType<CorrectionActivity>().CountAsync(ct));

			return result;
		}

		public class TransactionQueryDto
		{
			public DateOnly StartDate { get; set; }
			public DateOnly EndDate { get; set; }
			public int AccountId { get; set; }
			public string Symbol { get; set; } = string.Empty;
			public List<string> TransactionTypes { get; set; } = [];
			public string SearchText { get; set; } = string.Empty;
			public string SortColumn { get; set; } = "Date";
			public bool SortAscending { get; set; } = true;
			public int PageNumber { get; set; } = 1;
			public int PageSize { get; set; } = 25;
		}

		public class TransactionDisplayModelDto
		{
			public long Id { get; set; }
			public DateTime Date { get; set; }
			public string Type { get; set; } = string.Empty;
			public string? Symbol { get; set; }
			public string? Name { get; set; }
			public string Description { get; set; } = string.Empty;
			public string TransactionId { get; set; } = string.Empty;
			public string AccountName { get; set; } = string.Empty;
			public decimal? Quantity { get; set; }
			public decimal? UnitPriceAmount { get; set; }
			public string UnitPriceCurrency { get; set; } = string.Empty;
			public decimal? AmountValue { get; set; }
			public string AmountCurrency { get; set; } = string.Empty;
			public decimal? TotalValueAmount { get; set; }
			public string TotalValueCurrency { get; set; } = string.Empty;
			public string Currency { get; set; } = string.Empty;
			public decimal? FeeAmount { get; set; }
			public string FeeCurrency { get; set; } = string.Empty;
			public decimal? TaxAmount { get; set; }
			public string TaxCurrency { get; set; } = string.Empty;
		}

		public class PaginatedTransactionResultDto
		{
			public List<TransactionDisplayModelDto> Transactions { get; set; } = [];
			public int TotalCount { get; set; }
			public int PageNumber { get; set; }
			public int PageSize { get; set; }
			public Dictionary<string, int> TransactionTypeBreakdown { get; set; } = [];
			public Dictionary<string, int> AccountBreakdown { get; set; } = [];
		}
	}
}
