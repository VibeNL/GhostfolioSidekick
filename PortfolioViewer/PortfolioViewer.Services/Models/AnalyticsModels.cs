using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.PortfolioViewer.Services.Models;

public class MonthlyData
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class BuySellVolumeAnalysis
{
    public string Currency { get; set; } = string.Empty;
    public decimal TotalBuyVolume { get; set; }
    public decimal TotalSellVolume { get; set; }
    public int TransactionCount { get; set; }
}

public class DividendIncomeAnalysis
{
    public string Currency { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AveragePayment { get; set; }
    public int PaymentCount { get; set; }
}

public class HoldingActivitySummary
{
    public string Symbol { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
    public DateTime? LatestActivity { get; set; }
    public List<string> ActivityTypes { get; set; } = [];
}

public class TimelineItem
{
    public DateTime Date { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AccountSummary
{
    public string Name { get; set; } = string.Empty;
    public Platform? Platform { get; set; }
    public int ActivitiesCount { get; set; }
    public string LatestBalanceDisplay { get; set; } = string.Empty;
}