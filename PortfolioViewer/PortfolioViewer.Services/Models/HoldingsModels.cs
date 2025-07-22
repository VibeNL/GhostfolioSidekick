namespace GhostfolioSidekick.PortfolioViewer.Services.Models;

public class HoldingPerformanceData
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetClass { get; set; } = string.Empty;
    public string? AssetSubClass { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public string? ISIN { get; set; }
    public int ActivityCount { get; set; }
    public int MarketDataCount { get; set; }
    public string? LatestPrice { get; set; }
    public DateOnly? LatestPriceDate { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class AssetClassDistributionItem
{
    public string AssetClass { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class ActiveHoldingItem
{
    public string Symbol { get; set; } = string.Empty;
    public int ActivityCount { get; set; }
    public DateTime? LatestActivityDate { get; set; }
}

public class HoldingDetailsData
{
    public string Symbol { get; set; } = string.Empty;
    public List<ActivityItem> RecentActivities { get; set; } = [];
    public List<MarketDataItem> RecentMarketData { get; set; } = [];
}

public class ActivityItem
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
}

public class MarketDataItem
{
    public DateOnly Date { get; set; }
    public string Close { get; set; } = string.Empty;
    public string High { get; set; } = string.Empty;
    public string Low { get; set; } = string.Empty;
}