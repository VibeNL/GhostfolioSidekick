using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class DataIssuesService(DatabaseContext databaseContext) : IDataIssuesService
	{
		public async Task<List<DataIssueDisplayModel>> GetActivitiesWithoutHoldingsAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				// Query for activities that have no holding assigned - using more conservative approach
				var activitiesWithoutHoldings = await databaseContext.Activities
					.Include(a => a.Account)
					.Where(a => a.Holding == null)
					.OrderByDescending(a => a.Date)
					.ThenBy(a => a.Id)
					.ToListAsync(cancellationToken);

				// Only IActivityWithQuantityAndUnitPrice activities can have quantity and unit price
				activitiesWithoutHoldings = [.. activitiesWithoutHoldings.Where(a => a is IActivityWithPartialIdentifier)];

				var dataIssues = new List<DataIssueDisplayModel>();

				foreach (var activity in activitiesWithoutHoldings)
				{
					var dataIssue = new DataIssueDisplayModel
					{
						IssueType = "Activity Without Holding",
						Description = "This activity is not associated with any holding. It may need manual symbol matching or represents orphaned data.",
						Date = activity.Date,
						AccountName = activity.Account.Name ?? "Unknown",
						ActivityType = GetActivityTypeDisplayName(activity),
						TransactionId = activity.TransactionId,
						ActivityDescription = activity.Description,
						Severity = DetermineSeverityByTypeName(activity),
						Id = activity.Id,
					};

					dataIssues.Add(dataIssue);
				}

				// Now try to get more detailed information for activities if possible
				await EnrichWithDetailedActivityData(dataIssues, cancellationToken);

				return dataIssues;
			}
			catch (Exception ex)
			{
				// Log the exception and return empty list with error information
				return new List<DataIssueDisplayModel>
				{
					new DataIssueDisplayModel
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
				};
			}
		}

		private async Task EnrichWithDetailedActivityData(List<DataIssueDisplayModel> dataIssues, CancellationToken cancellationToken)
		{
			try
			{
				var activityIds = dataIssues.Select(d => d.Id).ToList();

				if (activityIds.Count == 0)
				{
					return;
				}

				var detailedActivities = await databaseContext.Activities
					.Where(a => activityIds.Contains(a.Id))
					.ToListAsync(cancellationToken);

				foreach (var dataIssue in dataIssues)
				{
					var activity = detailedActivities.FirstOrDefault(a => a.Id == dataIssue.Id);
					if (activity != null)
					{
						await EnrichDataIssueWithActivityDetails(dataIssue, activity);
					}
				}
			}
			catch
			{
				// If enrichment fails, continue with basic data
			}
		}

		private async Task EnrichDataIssueWithActivityDetails(DataIssueDisplayModel dataIssue, Activity activity)
		{
			try
			{
				// Try to get symbol identifiers if the activity has them
				if (activity is IActivityWithPartialIdentifier activityWithIdentifiers)
				{
					dataIssue.PartialIdentifiers = activityWithIdentifiers.PartialSymbolIdentifiers.ToList();
					dataIssue.SymbolIdentifiers = string.Join(", ", activityWithIdentifiers.PartialSymbolIdentifiers.Select(i => i.Identifier));
				}

				// Get quantity and unit price for activities that have them
				if (activity is ActivityWithQuantityAndUnitPrice quantityActivity)
				{
					dataIssue.Quantity = quantityActivity.Quantity;
					dataIssue.UnitPrice = quantityActivity.UnitPrice;
					dataIssue.Amount = quantityActivity.UnitPrice.Times(Math.Abs(quantityActivity.Quantity));
				}
				else if (activity is InterestActivity interestActivity)
				{
					dataIssue.Amount = interestActivity.Amount;
				}
				else if (activity is FeeActivity feeActivity)
				{
					dataIssue.Amount = feeActivity.Amount;
				}
				else if (activity is CashDepositWithdrawalActivity cashActivity)
				{
					dataIssue.Amount = cashActivity.Amount;
				}

				// Update activity type and severity based on actual activity
				dataIssue.ActivityType = GetActivityTypeName(activity);
				dataIssue.Severity = DetermineSeverity(activity);
			}
			catch
			{
				// If detailed enrichment fails, keep basic data
			}
		}

		private static string GetActivityTypeDisplayName(Activity activity)
		{
			var fullTypeName = activity.GetType().FullName ?? string.Empty;
			var activityName = fullTypeName.Split('.').LastOrDefault() ?? fullTypeName;
			return activityName.Replace("Activity", "").Replace("Proxy", "");
		}

		private static string GetActivityTypeName(Activity activity)
		{
			return activity switch
			{
				BuySellActivity buySell => buySell.Quantity > 0 ? "Buy" : "Sell",
				DividendActivity => "Dividend",
				CashDepositWithdrawalActivity cash => cash.Amount.Amount > 0 ? "Deposit" : "Withdrawal",
				FeeActivity => "Fee",
				InterestActivity => "Interest",
				SendAndReceiveActivity sendReceive => sendReceive.Quantity > 0 ? "Receive" : "Send",
				StakingRewardActivity => "Staking Reward",
				GiftAssetActivity => "Gift",
				GiftFiatActivity => "Gift Cash",
				ValuableActivity => "Valuable",
				LiabilityActivity => "Liability",
				KnownBalanceActivity => "Balance",
				RepayBondActivity => "Bond Repayment",
				_ => activity.GetType().Name.Replace("Activity", "")
			};
		}

		private static string DetermineSeverityByTypeName(Activity activity)
		{
			// Determine severity based on type name
			return activity.GetType().Name switch
			{
				var name when name.Contains("BuySell") => "Error",
				var name when name.Contains("Dividend") => "Error",
				var name when name.Contains("SendAndReceive") => "Error",
				var name when name.Contains("StakingReward") => "Error",
				var name when name.Contains("GiftAsset") => "Error",
				var name when name.Contains("CashDepositWithdrawal") => "Info",
				var name when name.Contains("Fee") => "Warning",
				var name when name.Contains("Interest") => "Info",
				_ => "Warning"
			};
		}

		private static string DetermineSeverity(Activity activity)
		{
			// Activities that typically need holdings are more severe
			return activity switch
			{
				BuySellActivity => "Error",
				DividendActivity => "Error",
				SendAndReceiveActivity => "Error",
				StakingRewardActivity => "Error",
				GiftAssetActivity => "Error",
				CashDepositWithdrawalActivity => "Info",
				FeeActivity => "Warning",
				InterestActivity => "Info",
				_ => "Warning"
			};
		}
	}
}