using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Performance;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Performance.Examples
{
    /// <summary>
    /// Example usage of the performance calculation service
    /// </summary>
    public class PerformanceCalculationExample
    {
        private readonly PerformanceCalculationService calculationService;

        public PerformanceCalculationExample(ILogger<PerformanceCalculationService> logger)
        {
            calculationService = new PerformanceCalculationService(logger);
        }

        /// <summary>
        /// Example: Calculate basic performance metrics for a portfolio
        /// </summary>
        public async Task<PerformanceMetrics> CalculateBasicPerformanceExample()
        {
            // Create sample holdings (in a real scenario, these would come from your database)
            var holdings = CreateSampleHoldings();

            // Define the performance period (last year)
            var period = PerformancePeriod.ForYear(2024);

            // Calculate performance metrics
            var metrics = await calculationService.CalculatePerformanceMetricsAsync(
                holdings, 
                period, 
                Currency.USD);

            return metrics;
        }

        /// <summary>
        /// Example: Generate a comprehensive performance report
        /// </summary>
        public async Task<PerformanceReport> GenerateComprehensiveReportExample()
        {
            var holdings = CreateSampleHoldings();
            var period = PerformancePeriod.LastDays(365);
            var benchmarks = new[]
            {
                PerformanceBenchmark.Common.SP500,
                PerformanceBenchmark.Common.NASDAQ
            };

            var report = await calculationService.GeneratePerformanceReportAsync(
                holdings, 
                period, 
                Currency.USD, 
                benchmarks);

            return report;
        }

        /// <summary>
        /// Example: Calculate risk metrics
        /// </summary>
        public async Task<RiskMetrics> CalculateRiskExample()
        {
            var holdings = CreateSampleHoldings();
            var period = PerformancePeriod.LastDays(252); // One trading year

            // Generate snapshots to create return series
            var snapshots = await calculationService.GeneratePerformanceSnapshotsAsync(
                holdings, 
                period.StartDate, 
                period.EndDate, 
                Currency.USD);

            // Create return series from snapshots
            var returns = snapshots
                .Where(s => s.DailyReturn.HasValue)
                .Select(s => new ReturnObservation(s.Date, s.DailyReturn!.Value))
                .ToList();

            var returnSeries = new ReturnSeries(returns, ReturnFrequency.Daily, Currency.USD);

            // Calculate risk metrics
            var riskMetrics = await calculationService.CalculateRiskMetricsAsync(returnSeries, period);

            return riskMetrics;
        }

        /// <summary>
        /// Example: Analyze performance attribution by holdings
        /// </summary>
        public async Task<IEnumerable<PerformanceAttribution>> AnalyzeAttributionExample()
        {
            var holdings = CreateSampleHoldings();
            var period = PerformancePeriod.ForMonth(2024, 12);

            var attributions = await calculationService.CalculatePerformanceAttributionAsync(
                holdings, 
                period, 
                Currency.USD);

            // Find top and bottom performers
            var topPerformers = attributions
                .OrderByDescending(a => a.ContributionToReturn)
                .Take(3);

            var bottomPerformers = attributions
                .OrderBy(a => a.ContributionToReturn)
                .Take(3);

            return attributions;
        }

        /// <summary>
        /// Example: Calculate portfolio allocation breakdown
        /// </summary>
        public async Task<PortfolioAllocation> AnalyzeAllocationExample()
        {
            var holdings = CreateSampleHoldings();
            var allocationDate = DateOnly.FromDateTime(DateTime.Today);

            var allocation = await calculationService.CalculatePortfolioAllocationAsync(
                holdings, 
                allocationDate, 
                Currency.USD);

            // Analyze allocation
            var largestHolding = allocation.MostConcentratedHolding;
            var cashPercentage = allocation.CashPercentage;
            var largestAssetClass = allocation.LargestAssetClassAllocation;

            return allocation;
        }

        /// <summary>
        /// Example: Compare against benchmarks
        /// </summary>
        public async Task<BenchmarkComparison> CompareToBenchmarkExample()
        {
            var holdings = CreateSampleHoldings();
            var period = PerformancePeriod.LastDays(252);

            // Generate portfolio return series
            var snapshots = await calculationService.GeneratePerformanceSnapshotsAsync(
                holdings, 
                period.StartDate, 
                period.EndDate, 
                Currency.USD);

            var portfolioReturns = CreateReturnSeries(snapshots, Currency.USD);
            
            // In a real scenario, you would fetch actual benchmark data
            var benchmarkReturns = CreateMockBenchmarkReturns(period, Currency.USD);

            var comparison = await calculationService.CalculateBenchmarkComparisonAsync(
                portfolioReturns,
                benchmarkReturns,
                PerformanceBenchmark.Common.SP500,
                period);

            return comparison;
        }

        /// <summary>
        /// Example: Identify drawdown periods
        /// </summary>
        public async Task<IEnumerable<Drawdown>> IdentifyDrawdownsExample()
        {
            var holdings = CreateSampleHoldings();
            var period = PerformancePeriod.LastDays(365);

            var snapshots = await calculationService.GeneratePerformanceSnapshotsAsync(
                holdings, 
                period.StartDate, 
                period.EndDate, 
                Currency.USD);

            var drawdowns = await calculationService.IdentifyDrawdownsAsync(snapshots);

            // Find the largest drawdown
            var maxDrawdown = drawdowns.MaxBy(d => d.DrawdownPercentage);

            return drawdowns;
        }

        // Helper methods for creating sample data
        private List<Holding> CreateSampleHoldings()
        {
            // Create sample holdings - in a real scenario, these would come from your database
            return new List<Holding>
            {
                new Holding { Id = 1 },
                new Holding { Id = 2 },
                new Holding { Id = 3 }
            };
        }

        private ReturnSeries CreateReturnSeries(IEnumerable<PerformanceSnapshot> snapshots, Currency currency)
        {
            var returns = snapshots
                .Where(s => s.DailyReturn.HasValue)
                .Select(s => new ReturnObservation(s.Date, s.DailyReturn!.Value, s.TotalValue))
                .ToList();

            return new ReturnSeries(returns, ReturnFrequency.Daily, currency);
        }

        private ReturnSeries CreateMockBenchmarkReturns(PerformancePeriod period, Currency currency)
        {
            // Create mock benchmark returns for demonstration
            var random = new Random(42); // Fixed seed for consistency
            var returns = new List<ReturnObservation>();
            var currentDate = period.StartDate;

            while (currentDate <= period.EndDate)
            {
                var dailyReturn = (decimal)(random.NextDouble() - 0.5) * 0.04m; // +/- 2% daily range
                returns.Add(new ReturnObservation(currentDate, dailyReturn));
                currentDate = currentDate.AddDays(1);
            }

            return new ReturnSeries(returns, ReturnFrequency.Daily, currency);
        }
    }

    /// <summary>
    /// Console application example showing how to use the performance calculations
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Setup logging
            using var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            var logger = loggerFactory.CreateLogger<PerformanceCalculationService>();
            
            // Create example instance
            var example = new PerformanceCalculationExample(logger);

            try
            {
                Console.WriteLine("=== Portfolio Performance Analysis Example ===\n");

                // Basic performance metrics
                Console.WriteLine("1. Calculating basic performance metrics...");
                var metrics = await example.CalculateBasicPerformanceExample();
                Console.WriteLine($"   Total Return: {metrics.TotalReturnPercentage:P2}");
                Console.WriteLine($"   Annualized Return: {metrics.AnnualizedReturn:P2}");
                Console.WriteLine($"   Volatility: {metrics.Volatility:P2}");
                Console.WriteLine($"   Sharpe Ratio: {metrics.SharpeRatio:F2}");
                Console.WriteLine($"   Max Drawdown: {metrics.MaxDrawdown:P2}\n");

                // Risk analysis
                Console.WriteLine("2. Analyzing risk metrics...");
                var riskMetrics = await example.CalculateRiskExample();
                Console.WriteLine($"   Standard Deviation: {riskMetrics.StandardDeviation:P2}");
                Console.WriteLine($"   Value at Risk (95%): {riskMetrics.ValueAtRisk95:P2}");
                Console.WriteLine($"   Sortino Ratio: {riskMetrics.SortinoRatio:F2}\n");

                // Attribution analysis
                Console.WriteLine("3. Performance attribution analysis...");
                var attributions = await example.AnalyzeAttributionExample();
                Console.WriteLine($"   Analyzed {attributions.Count()} holdings");
                Console.WriteLine($"   Top performer contribution: {attributions.Max(a => a.ContributionToReturn):P2}");
                Console.WriteLine($"   Bottom performer contribution: {attributions.Min(a => a.ContributionToReturn):P2}\n");

                // Allocation analysis
                Console.WriteLine("4. Portfolio allocation analysis...");
                var allocation = await example.AnalyzeAllocationExample();
                Console.WriteLine($"   Total portfolio value: {allocation.TotalValue}");
                Console.WriteLine($"   Cash percentage: {allocation.CashPercentage:P2}");
                Console.WriteLine($"   Number of holdings: {allocation.HoldingAllocations.Count}\n");

                // Benchmark comparison
                Console.WriteLine("5. Benchmark comparison...");
                var benchmarkComparison = await example.CompareToBenchmarkExample();
                Console.WriteLine($"   Alpha vs {benchmarkComparison.Benchmark.Name}: {benchmarkComparison.Alpha:P2}");
                Console.WriteLine($"   Beta: {benchmarkComparison.Beta:F2}");
                Console.WriteLine($"   Correlation: {benchmarkComparison.Correlation:F2}\n");

                // Drawdown analysis
                Console.WriteLine("6. Drawdown analysis...");
                var drawdowns = await example.IdentifyDrawdownsExample();
                var maxDrawdown = drawdowns.MaxBy(d => d.DrawdownPercentage);
                if (maxDrawdown != null)
                {
                    Console.WriteLine($"   Largest drawdown: {maxDrawdown.DrawdownPercentage:P2}");
                    Console.WriteLine($"   Drawdown period: {maxDrawdown.StartDate} to {maxDrawdown.EndDate}");
                }

                Console.WriteLine("\n=== Analysis Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during analysis: {ex.Message}");
            }
        }
    }
}