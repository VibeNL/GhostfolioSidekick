using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class PerformanceAnalytics
    {
        [Inject]
        private DatabaseContext DbContext { get; set; } = default!;

        private bool isLoading = true;
        private List<MonthlyData>? MonthlyActivityData;
        private List<BuySellVolumeAnalysis>? BuySellAnalysis;
        private List<DividendIncomeAnalysis>? DividendAnalysis;
        private List<HoldingActivitySummary>? TopActiveHoldings;
        private List<TimelineItem>? ActivityTimeline;

        protected override async Task OnInitializedAsync()
        {
            await LoadAnalyticsData();
            isLoading = false;
        }

        private async Task LoadAnalyticsData()
        {
            // Load monthly activity data
            var activities = await DbContext.Activities.ToListAsync();
            MonthlyActivityData = activities
                .GroupBy(a => new { a.Date.Year, a.Date.Month })
                .Select(g => new MonthlyData
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count()
                })
                .OrderByDescending(m => m.Month)
                .ToList();

            // Load buy/sell analysis
            var buySellActivities = await DbContext.Activities
                .OfType<BuySellActivity>()
                .ToListAsync();

            BuySellAnalysis = buySellActivities
                .GroupBy(a => a.TotalTransactionAmount.Currency.Symbol)
                .Select(g => new BuySellVolumeAnalysis
                {
                    Currency = g.Key,
                    TotalBuyVolume = g.Where(a => a.Quantity > 0).Sum(a => a.TotalTransactionAmount.Amount),
                    TotalSellVolume = g.Where(a => a.Quantity < 0).Sum(a => Math.Abs(a.TotalTransactionAmount.Amount)),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(a => a.TotalBuyVolume + a.TotalSellVolume)
                .ToList();

            // Load dividend analysis
            var dividendActivities = await DbContext.Activities
                .OfType<DividendActivity>()
                .ToListAsync();

            DividendAnalysis = dividendActivities
                .GroupBy(a => a.Amount.Currency.Symbol)
                .Select(g => new DividendIncomeAnalysis
                {
                    Currency = g.Key,
                    TotalAmount = g.Sum(a => a.Amount.Amount),
                    AveragePayment = g.Average(a => a.Amount.Amount),
                    PaymentCount = g.Count()
                })
                .OrderByDescending(a => a.TotalAmount)
                .ToList();

            // Load top active holdings - Fixed to avoid client projection with instance method
            var holdings = await DbContext.Holdings
                .Include(h => h.SymbolProfiles)
                .Include(h => h.Activities)
                .ToListAsync();

            TopActiveHoldings = holdings
                .Where(h => h.Activities.Any())
                .Select(h => new HoldingActivitySummary
                {
                    Symbol = h.SymbolProfiles.FirstOrDefault()?.Symbol ?? "Unknown",
                    AssetClass = h.SymbolProfiles.FirstOrDefault()?.AssetClass.ToString() ?? "Unknown",
                    ActivityCount = h.Activities.Count,
                    LatestActivity = h.Activities.Max(a => a.Date),
                    ActivityTypes = h.Activities.Select(a => GetActivityTypeName(a)).Distinct().ToList()
                })
                .OrderByDescending(h => h.ActivityCount)
                .Take(10)
                .ToList();

            // Load activity timeline - Fixed to avoid client projection with instance method
            var timelineActivities = await DbContext.Activities
                .Include(a => a.Account)
                .OrderByDescending(a => a.Date)
                .Take(50)
                .ToListAsync();

            ActivityTimeline = timelineActivities
                .Select(a => new TimelineItem
                {
                    Date = a.Date,
                    ActivityType = GetActivityTypeName(a),
                    Account = a.Account.Name,
                    Description = a.Description ?? "No description"
                })
                .ToList();
        }

        private static string GetActivityTypeName(Activity activity)
        {
            return activity.GetType().Name switch
            {
                nameof(BuySellActivity) => "Buy/Sell",
                nameof(DividendActivity) => "Dividend",
                nameof(CashDepositWithdrawalActivity) => "Cash",
                nameof(FeeActivity) => "Fee",
                nameof(InterestActivity) => "Interest",
                _ => activity.GetType().Name.Replace("Activity", "")
            };
        }

        private string GetActivityBadgeClass(string activityType)
        {
            return activityType switch
            {
                "Buy/Sell" => "bg-primary",
                "Dividend" => "bg-success",
                "Cash" => "bg-info",
                "Fee" => "bg-warning",
                "Interest" => "bg-secondary",
                _ => "bg-light text-dark"
            };
        }

        private string GetTimelineBorderClass(string activityType)
        {
            return activityType switch
            {
                "Buy/Sell" => "border-primary",
                "Dividend" => "border-success",
                "Cash" => "border-info",
                "Fee" => "border-warning",
                "Interest" => "border-secondary",
                _ => "border-light"
            };
        }

        private class MonthlyData
        {
            public string Month { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        private class BuySellVolumeAnalysis
        {
            public string Currency { get; set; } = string.Empty;
            public decimal TotalBuyVolume { get; set; }
            public decimal TotalSellVolume { get; set; }
            public int TransactionCount { get; set; }
        }

        private class DividendIncomeAnalysis
        {
            public string Currency { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public decimal AveragePayment { get; set; }
            public int PaymentCount { get; set; }
        }

        private class HoldingActivitySummary
        {
            public string Symbol { get; set; } = string.Empty;
            public string AssetClass { get; set; } = string.Empty;
            public int ActivityCount { get; set; }
            public DateTime? LatestActivity { get; set; }
            public List<string> ActivityTypes { get; set; } = [];
        }

        private class TimelineItem
        {
            public DateTime Date { get; set; }
            public string ActivityType { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}