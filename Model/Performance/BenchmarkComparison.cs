namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents performance metrics compared to a benchmark
    /// </summary>
    public record class BenchmarkComparison
    {
        /// <summary>
        /// The benchmark being compared against
        /// </summary>
        public PerformanceBenchmark Benchmark { get; init; } = null!;

        /// <summary>
        /// The time period for this comparison
        /// </summary>
        public PerformancePeriod Period { get; init; } = null!;

        /// <summary>
        /// Portfolio return percentage for the period
        /// </summary>
        public decimal PortfolioReturn { get; init; }

        /// <summary>
        /// Benchmark return percentage for the period
        /// </summary>
        public decimal BenchmarkReturn { get; init; }

        /// <summary>
        /// Alpha (excess return over benchmark)
        /// </summary>
        public decimal Alpha { get; init; }

        /// <summary>
        /// Beta (sensitivity to benchmark movements)
        /// </summary>
        public decimal Beta { get; init; }

        /// <summary>
        /// Correlation coefficient with the benchmark
        /// </summary>
        public decimal Correlation { get; init; }

        /// <summary>
        /// Tracking error (standard deviation of excess returns)
        /// </summary>
        public decimal TrackingError { get; init; }

        /// <summary>
        /// Information ratio (alpha / tracking error)
        /// </summary>
        public decimal? InformationRatio { get; init; }

        /// <summary>
        /// Upside capture ratio (performance during benchmark up periods)
        /// </summary>
        public decimal? UpsideCapture { get; init; }

        /// <summary>
        /// Downside capture ratio (performance during benchmark down periods)
        /// </summary>
        public decimal? DownsideCapture { get; init; }

        /// <summary>
        /// Maximum relative drawdown vs benchmark
        /// </summary>
        public decimal MaxRelativeDrawdown { get; init; }

        /// <summary>
        /// When this comparison was calculated
        /// </summary>
        public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;

        public BenchmarkComparison(
            PerformanceBenchmark benchmark,
            PerformancePeriod period,
            decimal portfolioReturn,
            decimal benchmarkReturn)
        {
            Benchmark = benchmark;
            Period = period;
            PortfolioReturn = portfolioReturn;
            BenchmarkReturn = benchmarkReturn;
            Alpha = portfolioReturn - benchmarkReturn;
        }

        /// <summary>
        /// Returns whether the portfolio outperformed the benchmark
        /// </summary>
        public bool OutperformedBenchmark => Alpha > 0;

        /// <summary>
        /// Returns the relative performance as a percentage
        /// </summary>
        public decimal RelativePerformance => (PortfolioReturn - BenchmarkReturn) * 100;
    }
}