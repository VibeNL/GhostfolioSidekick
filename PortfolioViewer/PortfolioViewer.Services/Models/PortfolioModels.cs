namespace GhostfolioSidekick.PortfolioViewer.Services.Models;

public class PortfolioValuePoint
{
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
    public decimal CashValue { get; set; }
    public decimal HoldingsValue { get; set; }
    public decimal CumulativeInvested { get; set; }
}

public class CashFlowPoint
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsDeposit { get; set; }
}

public class BalancePoint
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int AccountId { get; set; }
}

public class HoldingValuePoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public int HoldingId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}

public class AccountBreakdown
{
    public string AccountName { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal HoldingsValue { get; set; }
    public decimal PercentageOfPortfolio { get; set; }
}

public class PortfolioSummary
{
    public string CurrentPortfolioValue { get; set; } = "N/A";
    public string CurrentValueDate { get; set; } = "N/A";
    public decimal TotalReturnAmount { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public string TotalInvestedAmount { get; set; } = "N/A";
}