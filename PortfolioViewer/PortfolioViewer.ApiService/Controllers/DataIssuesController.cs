using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class DataIssuesController(IDbContextFactory<DatabaseContext> dbContextFactory) : ControllerBase
	{
		[HttpGet]
		public async Task<IActionResult> GetActivitiesWithoutHoldings(CancellationToken cancellationToken)
		{
			try
			{
				using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
				var activitiesWithoutHoldings = await db.Activities
					.Include(a => a.Account)
					.Where(a => a.Holding == null)
					.OrderByDescending(a => a.Date)
					.ThenBy(a => a.Id)
					.ToListAsync(cancellationToken);

				activitiesWithoutHoldings = [.. activitiesWithoutHoldings.Where(a => a is IActivityWithPartialIdentifier)];

				var result = activitiesWithoutHoldings.Select(activity =>
				{
					var dto = new DataIssueDisplayModelDto
					{
						Id = activity.Id,
						IssueType = "Activity Without Holding",
						Description = "This activity is not associated with any holding. It may need manual symbol matching or represents orphaned data.",
						Date = activity.Date,
						AccountName = activity.Account.Name ?? "Unknown",
						ActivityType = GetActivityTypeName(activity),
						Severity = DetermineSeverity(activity),
						TransactionId = activity.TransactionId ?? string.Empty,
						ActivityDescription = activity.Description,
					};

					if (activity is IActivityWithPartialIdentifier withIdentifiers)
					{
						dto.SymbolIdentifiers = string.Join(", ", withIdentifiers.PartialSymbolIdentifiers.Select(i => i.Identifier));
					}

					if (activity is ActivityWithQuantityAndUnitPrice q)
					{
						dto.Quantity = q.Quantity;
						dto.UnitPriceAmount = q.UnitPrice.Amount;
						dto.UnitPriceCurrency = q.UnitPrice.Currency.Symbol;
						dto.AmountValue = q.UnitPrice.Times(q.Quantity)?.Amount;
						dto.AmountCurrency = q.UnitPrice.Currency.Symbol;
					}
					else if (activity is InterestActivity interest)
					{
						dto.AmountValue = interest.Amount?.Amount;
						dto.AmountCurrency = interest.Amount?.Currency.Symbol ?? "";
					}
					else if (activity is FeeActivity fee)
					{
						dto.AmountValue = fee.Amount?.Amount;
						dto.AmountCurrency = fee.Amount?.Currency.Symbol ?? "";
					}
					else if (activity is CashDepositActivity deposit)
					{
						dto.AmountValue = deposit.Amount?.Amount;
						dto.AmountCurrency = deposit.Amount?.Currency.Symbol ?? "";
					}
					else if (activity is CashWithdrawalActivity withdrawal)
					{
						dto.AmountValue = withdrawal.Amount?.Amount;
						dto.AmountCurrency = withdrawal.Amount?.Currency.Symbol ?? "";
					}

					return dto;
				}).ToList();

				return Ok(result);
			}
			catch (Exception ex)
			{
				return Ok(new List<DataIssueDisplayModelDto>
				{
					new()
					{
						IssueType = "System Error",
						Description = $"Failed to analyze data issues: {ex.Message}",
						Date = DateTime.Now,
						AccountName = "System",
						ActivityType = "Error",
						TransactionId = "ERROR",
						ActivityDescription = ex.ToString(),
						Severity = "Error"
					}
				});
			}
		}

		private static string GetActivityTypeName(Activity activity) => activity switch
		{
			BuyActivity => "Buy",
			SellActivity => "Sell",
			DividendActivity => "Dividend",
			CashDepositActivity => "Deposit",
			CashWithdrawalActivity => "Withdrawal",
			FeeActivity => "Fee",
			InterestActivity => "Interest",
			ReceiveActivity => "Receive",
			SendActivity => "Send",
			StakingRewardActivity => "Staking Reward",
			GiftAssetActivity => "Gift",
			GiftFiatActivity => "Gift Cash",
			ValuableActivity => "Valuable",
			LiabilityActivity => "Liability",
			KnownBalanceActivity => "Balance",
			RepayBondActivity => "Bond Repayment",
			_ => activity.GetType().Name.Replace("Activity", "")
		};

		private static string DetermineSeverity(Activity activity) => activity switch
		{
			BuyActivity => "Error",
			SellActivity => "Error",
			DividendActivity => "Error",
			ReceiveActivity => "Error",
			SendActivity => "Error",
			StakingRewardActivity => "Error",
			GiftAssetActivity => "Error",
			CashDepositActivity => "Info",
			CashWithdrawalActivity => "Info",
			FeeActivity => "Warning",
			InterestActivity => "Info",
			_ => "Warning"
		};

		public class DataIssueDisplayModelDto
		{
			public long Id { get; set; }
			public string IssueType { get; set; } = string.Empty;
			public string Description { get; set; } = string.Empty;
			public DateTime Date { get; set; }
			public string AccountName { get; set; } = string.Empty;
			public string ActivityType { get; set; } = string.Empty;
			public string? Symbol { get; set; }
			public string? SymbolIdentifiers { get; set; }
			public decimal? Quantity { get; set; }
			public decimal? UnitPriceAmount { get; set; }
			public string UnitPriceCurrency { get; set; } = string.Empty;
			public decimal? AmountValue { get; set; }
			public string AmountCurrency { get; set; } = string.Empty;
			public string TransactionId { get; set; } = string.Empty;
			public string? ActivityDescription { get; set; }
			public string Severity { get; set; } = "Warning";
		}
	}
}
