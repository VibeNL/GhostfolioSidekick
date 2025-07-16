using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class PortfolioOverview
    {
        [Inject]
        private IPortfolioOverviewService PortfolioOverviewService { get; set; } = default!;

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
            try
            {
                // Load all overview data using the service
                var tasks = new[]
                {
                    LoadBasicStatisticsAsync(),
                    LoadActivityBreakdownAsync(),
                    LoadRecentActivitiesAsync(),
                    LoadAccountSummariesAsync()
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading overview data: {ex.Message}");
                await LoadDataSafely();
            }
        }

        private async Task LoadDataSafely()
        {
            // Load data safely with individual try-catch blocks
            try
            {
                await LoadBasicStatisticsAsync();
            }
            catch { /* Set defaults handled in method */ }

            try
            {
                ActivityBreakdown = await PortfolioOverviewService.GetActivityBreakdownAsync();
            }
            catch { ActivityBreakdown = new Dictionary<string, int>(); }

            try
            {
                RecentActivities = await PortfolioOverviewService.GetRecentActivitiesAsync();
            }
            catch { RecentActivities = new List<Activity>(); }

            try
            {
                AccountSummaries = await PortfolioOverviewService.GetAccountSummariesAsync();
            }
            catch { AccountSummaries = new List<AccountSummary>(); }
        }

        private async Task LoadBasicStatisticsAsync()
        {
            try
            {
                var tasks = new[]
                {
                    GetTotalAccountsAsync(),
                    GetTotalHoldingsAsync(),
                    GetTotalActivitiesAsync(),
                    GetBuyTransactionsAsync()
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading basic statistics: {ex.Message}");
                TotalAccounts = 0;
                TotalHoldings = 0;
                TotalActivities = 0;
                BuyTransactions = 0;
            }
        }

        private async Task GetTotalAccountsAsync()
        {
            try
            {
                TotalAccounts = await PortfolioOverviewService.GetTotalAccountsAsync();
            }
            catch { TotalAccounts = 0; }
        }

        private async Task GetTotalHoldingsAsync()
        {
            try
            {
                TotalHoldings = await PortfolioOverviewService.GetTotalHoldingsAsync();
            }
            catch { TotalHoldings = 0; }
        }

        private async Task GetTotalActivitiesAsync()
        {
            try
            {
                TotalActivities = await PortfolioOverviewService.GetTotalActivitiesAsync();
            }
            catch { TotalActivities = 0; }
        }

        private async Task GetBuyTransactionsAsync()
        {
            try
            {
                BuyTransactions = await PortfolioOverviewService.GetBuyTransactionsCountAsync();
            }
            catch { BuyTransactions = 0; }
        }

        private async Task LoadActivityBreakdownAsync()
        {
            ActivityBreakdown = await PortfolioOverviewService.GetActivityBreakdownAsync();
        }

        private async Task LoadRecentActivitiesAsync()
        {
            RecentActivities = await PortfolioOverviewService.GetRecentActivitiesAsync();
        }

        private async Task LoadAccountSummariesAsync()
        {
            AccountSummaries = await PortfolioOverviewService.GetAccountSummariesAsync();
        }

        private string GetActivityTypeName(Activity activity)
        {
            if (activity == null)
            {
                return "Unknown Activity";
            }

            return activity.GetType().Name switch
            {
                "BuySellActivity" => "Buy/Sell",
                "DividendActivity" => "Dividend",
                "CashDepositWithdrawalActivity" => "Cash",
                "FeeActivity" => "Fee",
                "InterestActivity" => "Interest",
                _ => activity.GetType().Name.Replace("Activity", "")
            };
        }
    }
}