using GhostfolioSidekick.PortfolioViewer.Services.Models;

namespace GhostfolioSidekick.PortfolioViewer.Services.Interfaces;

public interface IPerformanceAnalyticsService
{
    Task<List<MonthlyData>> GetMonthlyActivityDataAsync();
    Task<List<BuySellVolumeAnalysis>> GetBuySellAnalysisAsync();
    Task<List<DividendIncomeAnalysis>> GetDividendAnalysisAsync();
    Task<List<HoldingActivitySummary>> GetTopActiveHoldingsAsync(int count = 10);
    Task<List<TimelineItem>> GetActivityTimelineAsync(int count = 50);
}

public interface IPortfolioOverviewService
{
    Task<int> GetTotalAccountsAsync();
    Task<int> GetTotalHoldingsAsync();
    Task<int> GetTotalActivitiesAsync();
    Task<int> GetBuyTransactionsCountAsync();
    Task<Dictionary<string, int>> GetActivityBreakdownAsync();
    Task<List<GhostfolioSidekick.Model.Activities.Activity>> GetRecentActivitiesAsync(int count = 10);
    Task<List<AccountSummary>> GetAccountSummariesAsync();
}