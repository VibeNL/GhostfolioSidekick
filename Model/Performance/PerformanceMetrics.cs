using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents performance metrics for a portfolio, account, or holding over a specific time period
    /// </summary>
    public record class PerformanceMetrics
    {
        /// <summary>
        /// The time period for these performance metrics
        /// </summary>
        public PerformancePeriod Period { get; init; } = null!;

        /// <summary>
        /// The account these metrics relate to (optional - null for portfolio-wide metrics)
        /// </summary>
        public Account? Account { get; init; }

        /// <summary>
        /// The holding these metrics relate to (optional - null for account/portfolio-wide metrics)
        /// </summary>
        public Holding? Holding { get; init; }

        /// <summary>
        /// Total return as a money amount in the base currency
        /// </summary>
        public Money TotalReturn { get; init; } = new Money();

        /// <summary>
        /// Total return as a percentage
        /// </summary>
        public decimal TotalReturnPercentage { get; init; }

        /// <summary>
        /// Annualized return percentage
        /// </summary>
        public decimal AnnualizedReturn { get; init; }

        /// <summary>
        /// Volatility (standard deviation of returns)
        /// </summary>
        public decimal Volatility { get; init; }

        /// <summary>
        /// Sharpe ratio (risk-adjusted return)
        /// </summary>
        public decimal? SharpeRatio { get; init; }

        /// <summary>
        /// Maximum drawdown percentage
        /// </summary>
        public decimal MaxDrawdown { get; init; }

        /// <summary>
        /// Value at Risk (VaR) at 95% confidence level
        /// </summary>
        public Money? ValueAtRisk95 { get; init; }

        /// <summary>
        /// Total fees paid during the period
        /// </summary>
        public Money TotalFees { get; init; } = new Money();

        /// <summary>
        /// Total dividends received during the period
        /// </summary>
        public Money TotalDividends { get; init; } = new Money();

        /// <summary>
        /// Net cash flow during the period (deposits - withdrawals)
        /// </summary>
        public Money NetCashFlow { get; init; } = new Money();

        /// <summary>
        /// Starting value at the beginning of the period
        /// </summary>
        public Money StartingValue { get; init; } = new Money();

        /// <summary>
        /// Ending value at the end of the period
        /// </summary>
        public Money EndingValue { get; init; } = new Money();

        /// <summary>
        /// Time-weighted return percentage
        /// </summary>
        public decimal TimeWeightedReturn { get; init; }

        /// <summary>
        /// Money-weighted return (IRR) percentage
        /// </summary>
        public decimal? MoneyWeightedReturn { get; init; }

        /// <summary>
        /// When these metrics were calculated
        /// </summary>
        public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;
    }
}