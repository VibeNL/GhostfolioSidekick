namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents risk metrics for portfolio analysis
    /// </summary>
    public record class RiskMetrics
    {
        /// <summary>
        /// The time period for these risk metrics
        /// </summary>
        public PerformancePeriod Period { get; init; } = null!;

        /// <summary>
        /// Standard deviation of returns (volatility)
        /// </summary>
        public decimal StandardDeviation { get; init; }

        /// <summary>
        /// Annualized volatility
        /// </summary>
        public decimal AnnualizedVolatility { get; init; }

        /// <summary>
        /// Downside deviation (volatility of negative returns only)
        /// </summary>
        public decimal DownsideDeviation { get; init; }

        /// <summary>
        /// Value at Risk at 95% confidence level
        /// </summary>
        public decimal ValueAtRisk95 { get; init; }

        /// <summary>
        /// Value at Risk at 99% confidence level
        /// </summary>
        public decimal ValueAtRisk99 { get; init; }

        /// <summary>
        /// Conditional Value at Risk (Expected Shortfall) at 95%
        /// </summary>
        public decimal ConditionalVaR95 { get; init; }

        /// <summary>
        /// Maximum drawdown percentage
        /// </summary>
        public decimal MaxDrawdown { get; init; }

        /// <summary>
        /// Average drawdown percentage
        /// </summary>
        public decimal AverageDrawdown { get; init; }

        /// <summary>
        /// Maximum drawdown duration in days
        /// </summary>
        public int MaxDrawdownDuration { get; init; }

        /// <summary>
        /// Skewness of return distribution
        /// </summary>
        public decimal Skewness { get; init; }

        /// <summary>
        /// Kurtosis of return distribution
        /// </summary>
        public decimal Kurtosis { get; init; }

        /// <summary>
        /// Sortino ratio (return/downside deviation)
        /// </summary>
        public decimal? SortinoRatio { get; init; }

        /// <summary>
        /// Calmar ratio (annualized return/max drawdown)
        /// </summary>
        public decimal? CalmarRatio { get; init; }

        /// <summary>
        /// Percentage of positive return periods
        /// </summary>
        public decimal PositivePeriods { get; init; }

        /// <summary>
        /// Largest single period gain
        /// </summary>
        public decimal LargestGain { get; init; }

        /// <summary>
        /// Largest single period loss
        /// </summary>
        public decimal LargestLoss { get; init; }

        /// <summary>
        /// Average positive return
        /// </summary>
        public decimal AverageGain { get; init; }

        /// <summary>
        /// Average negative return
        /// </summary>
        public decimal AverageLoss { get; init; }

        /// <summary>
        /// Gain/loss ratio
        /// </summary>
        public decimal? GainLossRatio { get; init; }

        /// <summary>
        /// When these metrics were calculated
        /// </summary>
        public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;

        public RiskMetrics(PerformancePeriod period, decimal standardDeviation, decimal maxDrawdown)
        {
            Period = period;
            StandardDeviation = standardDeviation;
            MaxDrawdown = maxDrawdown;
        }

        /// <summary>
        /// Returns a risk assessment based on volatility levels
        /// </summary>
        public RiskLevel GetRiskLevel()
        {
            return AnnualizedVolatility switch
            {
                < 0.05m => RiskLevel.VeryLow,
                < 0.10m => RiskLevel.Low,
                < 0.15m => RiskLevel.Moderate,
                < 0.25m => RiskLevel.High,
                _ => RiskLevel.VeryHigh
            };
        }
    }

    /// <summary>
    /// Risk level enumeration
    /// </summary>
    public enum RiskLevel
    {
        VeryLow,
        Low,
        Moderate,
        High,
        VeryHigh
    }
}