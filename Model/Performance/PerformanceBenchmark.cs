namespace GhostfolioSidekick.Model.Performance
{
    /// <summary>
    /// Represents a benchmark used for performance comparison
    /// </summary>
    public record class PerformanceBenchmark
    {
        /// <summary>
        /// Unique identifier for the benchmark
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Display name of the benchmark
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Symbol or ticker of the benchmark
        /// </summary>
        public string Symbol { get; init; } = string.Empty;

        /// <summary>
        /// Data source for the benchmark (e.g., Yahoo, Bloomberg)
        /// </summary>
        public string DataSource { get; init; } = string.Empty;

        /// <summary>
        /// Currency of the benchmark
        /// </summary>
        public Currency Currency { get; init; } = Currency.USD;

        /// <summary>
        /// Description of what this benchmark represents
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Category of the benchmark (e.g., "Market Index", "Sector Index")
        /// </summary>
        public string? Category { get; init; }

        /// <summary>
        /// Whether this benchmark is active for performance comparisons
        /// </summary>
        public bool IsActive { get; init; } = true;

        public PerformanceBenchmark(string id, string name, string symbol, string dataSource, Currency currency)
        {
            Id = id;
            Name = name;
            Symbol = symbol;
            DataSource = dataSource;
            Currency = currency;
        }

        /// <summary>
        /// Creates a common market index benchmark
        /// </summary>
        public static PerformanceBenchmark CreateMarketIndex(string symbol, string name, Currency currency, string dataSource = "YAHOO")
        {
            return new PerformanceBenchmark(symbol, name, symbol, dataSource, currency)
            {
                Category = "Market Index",
                Description = $"{name} market index"
            };
        }

        /// <summary>
        /// Common benchmarks
        /// </summary>
        public static class Common
        {
            public static PerformanceBenchmark SP500 => CreateMarketIndex("^GSPC", "S&P 500", Currency.USD);
            public static PerformanceBenchmark NASDAQ => CreateMarketIndex("^IXIC", "NASDAQ Composite", Currency.USD);
            public static PerformanceBenchmark DowJones => CreateMarketIndex("^DJI", "Dow Jones Industrial Average", Currency.USD);
            public static PerformanceBenchmark FTSE100 => CreateMarketIndex("^FTSE", "FTSE 100", Currency.GBP);
            public static PerformanceBenchmark DAX => CreateMarketIndex("^GDAXI", "DAX", Currency.EUR);
            public static PerformanceBenchmark EuroStoxx50 => CreateMarketIndex("^STOXX50E", "EURO STOXX 50", Currency.EUR);
            public static PerformanceBenchmark AEX => CreateMarketIndex("^AEX", "AEX", Currency.EUR);
        }
    }
}