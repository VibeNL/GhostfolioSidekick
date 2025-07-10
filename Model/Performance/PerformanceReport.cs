namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a comprehensive performance report combining various metrics
    /// </summary>
    public record class PerformanceReport
    {
        /// <summary>
        /// Basic performance metrics
        /// </summary>
        public PerformanceMetrics Metrics { get; init; } = null!;

        /// <summary>
        /// Risk analysis
        /// </summary>
        public RiskMetrics RiskMetrics { get; init; } = null!;

        /// <summary>
        /// Performance attribution by holdings
        /// </summary>
        public IReadOnlyList<PerformanceAttribution> HoldingAttributions { get; init; } = [];

        /// <summary>
        /// Benchmark comparisons
        /// </summary>
        public IReadOnlyList<BenchmarkComparison> BenchmarkComparisons { get; init; } = [];

        /// <summary>
        /// Historical performance snapshots
        /// </summary>
        public IReadOnlyList<PerformanceSnapshot> PerformanceHistory { get; init; } = [];

        /// <summary>
        /// Performance by asset class
        /// </summary>
        public IReadOnlyDictionary<Activities.AssetClass, PerformanceMetrics> AssetClassPerformance { get; init; } = 
            new Dictionary<Activities.AssetClass, PerformanceMetrics>();

        /// <summary>
        /// Performance by sector (if applicable)
        /// </summary>
        public IReadOnlyDictionary<string, PerformanceMetrics> SectorPerformance { get; init; } = 
            new Dictionary<string, PerformanceMetrics>();

        /// <summary>
        /// Performance by country (if applicable)
        /// </summary>
        public IReadOnlyDictionary<string, PerformanceMetrics> CountryPerformance { get; init; } = 
            new Dictionary<string, PerformanceMetrics>();

        /// <summary>
        /// Currency exposure breakdown
        /// </summary>
        public IReadOnlyDictionary<Currency, decimal> CurrencyExposure { get; init; } = 
            new Dictionary<Currency, decimal>();

        /// <summary>
        /// When this report was generated
        /// </summary>
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Base currency for all calculations
        /// </summary>
        public Currency BaseCurrency { get; init; } = Currency.USD;

        /// <summary>
        /// The account this report relates to (optional - null for portfolio-wide)
        /// </summary>
        public Accounts.Account? Account { get; init; }

        /// <summary>
        /// Version of the calculation engine used
        /// </summary>
        public string CalculationVersion { get; init; } = "1.0";

        public PerformanceReport(
            PerformanceMetrics metrics,
            RiskMetrics riskMetrics,
            Currency baseCurrency)
        {
            Metrics = metrics;
            RiskMetrics = riskMetrics;
            BaseCurrency = baseCurrency;
        }

        /// <summary>
        /// Gets the top performing holdings by contribution
        /// </summary>
        public IEnumerable<PerformanceAttribution> GetTopPerformers(int count = 5)
        {
            return HoldingAttributions
                .OrderByDescending(x => x.ContributionToReturn)
                .Take(count);
        }

        /// <summary>
        /// Gets the worst performing holdings by contribution
        /// </summary>
        public IEnumerable<PerformanceAttribution> GetWorstPerformers(int count = 5)
        {
            return HoldingAttributions
                .OrderBy(x => x.ContributionToReturn)
                .Take(count);
        }

        /// <summary>
        /// Gets the best benchmark comparison
        /// </summary>
        public BenchmarkComparison? GetBestBenchmarkComparison()
        {
            return BenchmarkComparisons.MaxBy(x => x.Alpha);
        }

        /// <summary>
        /// Calculates the overall portfolio score based on various metrics
        /// </summary>
        public decimal CalculatePortfolioScore()
        {
            var returnScore = Math.Min(Metrics.AnnualizedReturn * 10, 100);
            var riskScore = Math.Max(100 - (RiskMetrics.AnnualizedVolatility * 100), 0);
            var sharpeScore = (Metrics.SharpeRatio ?? 0) * 20;
            
            return (returnScore + riskScore + sharpeScore) / 3;
        }
    }
}