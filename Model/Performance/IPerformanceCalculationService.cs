namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Domain service interface for performance calculations
    /// </summary>
    public interface IPerformanceCalculationService
    {
        /// <summary>
        /// Calculates performance metrics for a portfolio over a given period
        /// </summary>
        Task<PerformanceMetrics> CalculatePerformanceMetricsAsync(
            IEnumerable<Holding> holdings,
            PerformancePeriod period,
            Currency baseCurrency);

        /// <summary>
        /// Calculates performance metrics for a specific holding
        /// </summary>
        Task<PerformanceMetrics> CalculateHoldingPerformanceAsync(
            Holding holding,
            PerformancePeriod period,
            Currency baseCurrency);

        /// <summary>
        /// Calculates risk metrics for a portfolio
        /// </summary>
        Task<RiskMetrics> CalculateRiskMetricsAsync(
            ReturnSeries returnSeries,
            PerformancePeriod period);

        /// <summary>
        /// Calculates performance attribution for all holdings
        /// </summary>
        Task<IEnumerable<PerformanceAttribution>> CalculatePerformanceAttributionAsync(
            IEnumerable<Holding> holdings,
            PerformancePeriod period,
            Currency baseCurrency);

        /// <summary>
        /// Generates performance snapshots for a date range
        /// </summary>
        Task<IEnumerable<PerformanceSnapshot>> GeneratePerformanceSnapshotsAsync(
            IEnumerable<Holding> holdings,
            DateOnly startDate,
            DateOnly endDate,
            Currency baseCurrency);

        /// <summary>
        /// Calculates benchmark comparison
        /// </summary>
        Task<BenchmarkComparison> CalculateBenchmarkComparisonAsync(
            ReturnSeries portfolioReturns,
            ReturnSeries benchmarkReturns,
            PerformanceBenchmark benchmark,
            PerformancePeriod period);

        /// <summary>
        /// Generates a comprehensive performance report
        /// </summary>
        Task<PerformanceReport> GeneratePerformanceReportAsync(
            IEnumerable<Holding> holdings,
            PerformancePeriod period,
            Currency baseCurrency,
            IEnumerable<PerformanceBenchmark>? benchmarks = null);

        /// <summary>
        /// Calculates portfolio allocation breakdown
        /// </summary>
        Task<PortfolioAllocation> CalculatePortfolioAllocationAsync(
            IEnumerable<Holding> holdings,
            DateOnly date,
            Currency baseCurrency);

        /// <summary>
        /// Identifies drawdown periods
        /// </summary>
        Task<IEnumerable<Drawdown>> IdentifyDrawdownsAsync(
            IEnumerable<PerformanceSnapshot> snapshots);

        /// <summary>
        /// Calculates time-weighted returns
        /// </summary>
        Task<decimal> CalculateTimeWeightedReturnAsync(
            IEnumerable<TimeWeightedPeriod> periods);

        /// <summary>
        /// Calculates money-weighted return (IRR)
        /// </summary>
        Task<decimal?> CalculateMoneyWeightedReturnAsync(
            IEnumerable<CashFlow> cashFlows,
            Money initialValue,
            Money finalValue);
    }
}