using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class PerformanceAnalytics
    {
        [Inject]
        private IPerformanceAnalyticsService PerformanceAnalyticsService { get; set; } = default!;

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
            try
            {
                // Load all analytics data using the service
                var tasks = new[]
                {
                    LoadMonthlyActivityDataAsync(),
                    LoadBuySellAnalysisAsync(),
                    LoadDividendAnalysisAsync(),
                    LoadTopActiveHoldingsAsync(),
                    LoadActivityTimelineAsync()
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                // Log error in production
                Console.WriteLine($"Error loading analytics data: {ex.Message}");
            }
        }

        private async Task LoadMonthlyActivityDataAsync()
        {
            try
            {
                MonthlyActivityData = await PerformanceAnalyticsService.GetMonthlyActivityDataAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading monthly activity data: {ex.Message}");
                MonthlyActivityData = new List<MonthlyData>();
            }
        }

        private async Task LoadBuySellAnalysisAsync()
        {
            try
            {
                BuySellAnalysis = await PerformanceAnalyticsService.GetBuySellAnalysisAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading buy/sell analysis: {ex.Message}");
                BuySellAnalysis = new List<BuySellVolumeAnalysis>();
            }
        }

        private async Task LoadDividendAnalysisAsync()
        {
            try
            {
                DividendAnalysis = await PerformanceAnalyticsService.GetDividendAnalysisAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dividend analysis: {ex.Message}");
                DividendAnalysis = new List<DividendIncomeAnalysis>();
            }
        }

        private async Task LoadTopActiveHoldingsAsync()
        {
            try
            {
                TopActiveHoldings = await PerformanceAnalyticsService.GetTopActiveHoldingsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading top active holdings: {ex.Message}");
                TopActiveHoldings = new List<HoldingActivitySummary>();
            }
        }

        private async Task LoadActivityTimelineAsync()
        {
            try
            {
                ActivityTimeline = await PerformanceAnalyticsService.GetActivityTimelineAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading activity timeline: {ex.Message}");
                ActivityTimeline = new List<TimelineItem>();
            }
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
    }
}