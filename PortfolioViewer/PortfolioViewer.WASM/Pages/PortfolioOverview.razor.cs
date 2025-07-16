using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class PortfolioOverview
    {
        [Inject]
        private DatabaseContext DbContext { get; set; } = default!;

        private bool isLoading = true;
        private int TotalAccounts = 0;
        private int TotalHoldings = 0;
        private int TotalActivities = 0;
        private int BuyTransactions = 0;
        private Dictionary<string, int>? ActivityBreakdown;
        private List<Activity>? RecentActivities;
        private List<AccountSummary>? AccountSummaries;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            isLoading = false;
        }

        private async Task LoadData()
        {
            // Load basic statistics
            TotalAccounts = await DbContext.Accounts.CountAsync();
            TotalHoldings = await DbContext.Holdings.CountAsync();
            TotalActivities = await DbContext.Activities.CountAsync();
            BuyTransactions = await DbContext.Activities.OfType<BuySellActivity>().CountAsync();

            // Load activity breakdown
            var activities = await DbContext.Activities.ToListAsync();
            ActivityBreakdown = activities
                .GroupBy(a => GetActivityTypeName(a))
                .ToDictionary(g => g.Key, g => g.Count());

            // Load recent activities
            RecentActivities = await DbContext.Activities
                .Include(a => a.Account)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();

            // Load account summaries
            var accounts = await DbContext.Accounts
                .Include(a => a.Platform)
                .Include(a => a.Balance)
                .ToListAsync();

            var accountActivitiesCounts = await DbContext.Activities
                .GroupBy(a => a.Account.Id)
                .Select(g => new { AccountId = g.Key, Count = g.Count() })
                .ToListAsync();

            AccountSummaries = accounts.Select(account => new AccountSummary
            {
                Name = account.Name,
                Platform = account.Platform,
                ActivitiesCount = accountActivitiesCounts.FirstOrDefault(x => x.AccountId == account.Id)?.Count ?? 0,
                LatestBalanceDisplay = account.Balance
                    .OrderByDescending(b => b.Date)
                    .FirstOrDefault()?.Money.ToString() ?? "N/A"
            }).ToList();
        }

        private string GetActivityTypeName(Activity activity)
        {
			if (activity == null)
			{
				return "Unknown Activity";
			}

			if (activity is BuySellActivity)
			{
				return "Buy/Sell";
			}

			if (activity is DividendActivity)
			{
				return "Dividend";
			}

			if (activity is CashDepositWithdrawalActivity)
			{
				return "Cash Deposit/Withdrawal";
			}

			if (activity is FeeActivity)
			{
				return "Fee";
			}

			if (activity is InterestActivity)
			{
				return "Interest";
			}

			if (activity is GiftAssetActivity)
			{
				return "Gift Asset";
			}

			if (activity is GiftFiatActivity)
			{
				return "Gift Fiat";
			}

			if (activity is KnownBalanceActivity)
			{
				return "Known Balance";
			}

			if (activity is LiabilityActivity)
			{
				return "Liability";
			}

			if (activity is RepayBondActivity)
			{
				return "Repay Bond";
			}

			if (activity is ValuableActivity)
			{
				return "Valuable";
			}

			if (activity is SendAndReceiveActivity)
			{
				return "Send/Receive";
			}

			if (activity is StakingRewardActivity)
			{
				return "Staking Reward";
			}

			// Default case for any other activity type
			return activity.GetType().Name.Replace("Activity", string.Empty);
		}

        private class AccountSummary
        {
            public string Name { get; set; } = string.Empty;
            public GhostfolioSidekick.Model.Accounts.Platform? Platform { get; set; }
            public int ActivitiesCount { get; set; }
            public string LatestBalanceDisplay { get; set; } = string.Empty;
        }
    }
}