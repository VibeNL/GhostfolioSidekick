using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.Services.Implementation;

public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<PerformanceAnalyticsService> _logger;

    public PerformanceAnalyticsService(DatabaseContext dbContext, ILogger<PerformanceAnalyticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<MonthlyData>> GetMonthlyActivityDataAsync()
    {
        try
        {
            var activities = await _dbContext.Activities.ToListAsync();
            return activities
                .GroupBy(a => new { a.Date.Year, a.Date.Month })
                .Select(g => new MonthlyData
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count()
                })
                .OrderByDescending(m => m.Month)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly activity data");
            return new List<MonthlyData>();
        }
    }

    public async Task<List<BuySellVolumeAnalysis>> GetBuySellAnalysisAsync()
    {
        try
        {
            var buySellActivities = await _dbContext.Activities
                .OfType<BuySellActivity>()
                .ToListAsync();

            return buySellActivities
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading buy/sell analysis");
            return new List<BuySellVolumeAnalysis>();
        }
    }

    public async Task<List<DividendIncomeAnalysis>> GetDividendAnalysisAsync()
    {
        try
        {
            var dividendActivities = await _dbContext.Activities
                .OfType<DividendActivity>()
                .ToListAsync();

            return dividendActivities
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dividend analysis");
            return new List<DividendIncomeAnalysis>();
        }
    }

    public async Task<List<HoldingActivitySummary>> GetTopActiveHoldingsAsync(int count = 10)
    {
        try
        {
            var holdings = await _dbContext.Holdings
                .Include(h => h.SymbolProfiles)
                .Include(h => h.Activities)
                .ToListAsync();

            return holdings
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
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading top active holdings");
            return new List<HoldingActivitySummary>();
        }
    }

    public async Task<List<TimelineItem>> GetActivityTimelineAsync(int count = 50)
    {
        try
        {
            var timelineActivities = await _dbContext.Activities
                .Include(a => a.Account)
                .OrderByDescending(a => a.Date)
                .Take(count)
                .ToListAsync();

            return timelineActivities
                .Select(a => new TimelineItem
                {
                    Date = a.Date,
                    ActivityType = GetActivityTypeName(a),
                    Account = a.Account.Name,
                    Description = a.Description ?? "No description"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading activity timeline");
            return new List<TimelineItem>();
        }
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
}

public class PortfolioOverviewService : IPortfolioOverviewService
{
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<PortfolioOverviewService> _logger;

    public PortfolioOverviewService(DatabaseContext dbContext, ILogger<PortfolioOverviewService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> GetTotalAccountsAsync()
    {
        try
        {
            return await _dbContext.Accounts.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total accounts count");
            return 0;
        }
    }

    public async Task<int> GetTotalHoldingsAsync()
    {
        try
        {
            return await _dbContext.Holdings.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total holdings count");
            return 0;
        }
    }

    public async Task<int> GetTotalActivitiesAsync()
    {
        try
        {
            return await _dbContext.Activities.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total activities count");
            return 0;
        }
    }

    public async Task<int> GetBuyTransactionsCountAsync()
    {
        try
        {
            return await _dbContext.Activities.OfType<BuySellActivity>().CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting buy transactions count");
            return 0;
        }
    }

    public async Task<Dictionary<string, int>> GetActivityBreakdownAsync()
    {
        try
        {
            var activities = await _dbContext.Activities.ToListAsync();
            return activities
                .GroupBy(a => GetActivityTypeName(a))
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity breakdown");
            return new Dictionary<string, int>();
        }
    }

    public async Task<List<Activity>> GetRecentActivitiesAsync(int count = 10)
    {
        try
        {
            return await _dbContext.Activities
                .Include(a => a.Account)
                .OrderByDescending(a => a.Date)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities");
            return new List<Activity>();
        }
    }

    public async Task<List<AccountSummary>> GetAccountSummariesAsync()
    {
        try
        {
            var accounts = await _dbContext.Accounts
                .Include(a => a.Platform)
                .Include(a => a.Balance)
                .ToListAsync();

            var accountActivitiesCounts = await _dbContext.Activities
                .GroupBy(a => a.Account.Id)
                .Select(g => new { AccountId = g.Key, Count = g.Count() })
                .ToListAsync();

            return accounts.Select(account => new AccountSummary
            {
                Name = account.Name,
                Platform = account.Platform,
                ActivitiesCount = accountActivitiesCounts.FirstOrDefault(x => x.AccountId == account.Id)?.Count ?? 0,
                LatestBalanceDisplay = account.Balance
                    .OrderByDescending(b => b.Date)
                    .FirstOrDefault()?.Money.ToString() ?? "N/A"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account summaries");
            return new List<AccountSummary>();
        }
    }

    private static string GetActivityTypeName(Activity activity)
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
            return "Cash";
        }

        if (activity is FeeActivity)
        {
            return "Fee";
        }

        if (activity is InterestActivity)
        {
            return "Interest";
        }

        return activity.GetType().Name.Replace("Activity", "");
    }
}