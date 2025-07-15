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
            return activity.GetType().Name switch
            {
                nameof(BuySellActivity) => "Buy/Sell",
                nameof(DividendActivity) => "Dividend",
                nameof(CashDepositWithdrawalActivity) => "Cash Deposit/Withdrawal",
                nameof(FeeActivity) => "Fee",
                nameof(InterestActivity) => "Interest",
                nameof(GiftAssetActivity) => "Gift Asset",
                nameof(GiftFiatActivity) => "Gift Fiat",
                nameof(KnownBalanceActivity) => "Known Balance",
                nameof(LiabilityActivity) => "Liability",
                nameof(RepayBondActivity) => "Repay Bond",
                nameof(ValuableActivity) => "Valuable",
                nameof(SendAndReceiveActivity) => "Send/Receive",
                nameof(StakingRewardActivity) => "Staking Reward",
                _ => activity.GetType().Name
            };
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