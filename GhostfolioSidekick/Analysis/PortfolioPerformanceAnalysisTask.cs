using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Analysis
{
	public class PortfolioPerformanceAnalysisTask(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		PortfolioAnalysisService analysisService,
		MarketDataPortfolioPerformanceCalculator marketDataCalculator,
		ILogger<PortfolioPerformanceAnalysisTask> logger) : IScheduledWork
	{
		public string Name => "Portfolio Performance Analysis";

		public TaskPriority Priority => TaskPriority.DisplayInformation;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			logger.LogInformation("Starting enhanced portfolio performance analysis task with persistent storage");

			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				// Get all holdings with their activities and market data
				var holdings = await context.Holdings
					.Include(h => h.Activities)
					.Include(h => h.SymbolProfiles)
					.ThenInclude(sp => sp.MarketData)
					.ToListAsync();

				if (!holdings.Any())
				{
					logger.LogWarning("No holdings found for performance analysis");
					return;
				}

				logger.LogInformation("Found {HoldingCount} holdings for analysis", holdings.Count);

				// Check market data quality
				await AssessMarketDataQuality(holdings);

				// Generate standard performance report with persistent storage
				await GenerateStandardPerformanceReport(holdings, Currency.EUR);

				// Generate detailed analysis for individual holdings
				await GenerateDetailedHoldingAnalysis(holdings, Currency.EUR);

				// Display storage statistics and available periods
				await DisplayStorageStatistics();

				logger.LogInformation("Portfolio performance analysis completed successfully");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during portfolio performance analysis");
				throw;
			}
		}

		/// <summary>
		/// Assess the quality and coverage of market data
		/// </summary>
		private async Task AssessMarketDataQuality(List<Holding> holdings)
		{
			logger.LogInformation("=== Market Data Quality Assessment ===");

			var totalHoldings = holdings.Count;
			var holdingsWithMarketData = 0;
			var holdingsWithRecentData = 0;
			var totalMarketDataPoints = 0;

			foreach (var holding in holdings)
			{
				var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
				if (symbolProfile == null) continue;

				if (symbolProfile.MarketData != null && symbolProfile.MarketData.Any())
				{
					holdingsWithMarketData++;
					totalMarketDataPoints += symbolProfile.MarketData.Count;

					var latestData = symbolProfile.MarketData.OrderByDescending(md => md.Date).First();
					var daysSinceUpdate = (DateTime.Now.Date - latestData.Date.ToDateTime(TimeOnly.MinValue)).Days;

					if (daysSinceUpdate <= 5) // Consider data recent if within 5 days
					{
						holdingsWithRecentData++;
					}

					logger.LogDebug("{Symbol}: {DataPoints} market data points, latest: {LatestDate} ({Days} days ago)",
						symbolProfile.Symbol, symbolProfile.MarketData.Count, latestData.Date, daysSinceUpdate);
				}
				else
				{
					logger.LogWarning("{Symbol}: No market data available", symbolProfile.Symbol);
				}
			}

			var marketDataCoverage = (double)holdingsWithMarketData / totalHoldings * 100;
			var recentDataCoverage = (double)holdingsWithRecentData / totalHoldings * 100;

			logger.LogInformation("Market Data Coverage: {Coverage:F1}% ({WithData}/{Total} holdings)", 
				marketDataCoverage, holdingsWithMarketData, totalHoldings);
			logger.LogInformation("Recent Data Coverage: {Coverage:F1}% ({WithRecentData}/{Total} holdings)", 
				recentDataCoverage, holdingsWithRecentData, totalHoldings);
			logger.LogInformation("Total Market Data Points: {TotalPoints}", totalMarketDataPoints);

			if (marketDataCoverage < 80)
			{
				logger.LogWarning("Market data coverage is below 80%. Performance calculations may be less accurate.");
			}

			await Task.CompletedTask; // Placeholder for any async operations
		}

		/// <summary>
		/// Generate standard performance report using stored calculations
		/// </summary>
		private async Task GenerateStandardPerformanceReport(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("=== Standard Performance Report (Stored) ===");

			try
			{
				// Get standard performance reports with storage
				var performanceResults = await analysisService.GetStandardPerformanceReportAsync(
					holdings, baseCurrency, forceRecalculation: false);

				if (!performanceResults.Any())
				{
					logger.LogWarning("No performance data available for any standard periods");
					return;
				}

				foreach (var (periodName, performance) in performanceResults)
				{
					logger.LogInformation("{PeriodName}: TWR {TWR:F2}%, Dividends {Dividends}, Value {InitialValue} ? {FinalValue}",
						periodName, performance.TimeWeightedReturn, performance.TotalDividends, 
						performance.InitialValue, performance.FinalValue);
				}

				// Generate detailed summary for the most recent period
				var recentPeriod = performanceResults.FirstOrDefault();
				if (recentPeriod.Value != null)
				{
					logger.LogInformation("=== Detailed Summary for {PeriodName} ===", recentPeriod.Key);
					var summary = await analysisService.GeneratePerformanceSummaryAsync(
						holdings, recentPeriod.Value.StartDate, recentPeriod.Value.EndDate, baseCurrency, forceRecalculation: false);
					
					var lines = summary.Split('\n');
					foreach (var line in lines)
					{
						logger.LogInformation(line);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating standard performance report");
			}
		}

		/// <summary>
		/// Generate detailed analysis for individual holdings with storage
		/// </summary>
		private async Task GenerateDetailedHoldingAnalysis(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("=== Detailed Holding Analysis (Stored) ===");

			try
			{
				// Analyze top 10 holdings by activity count
				var topHoldings = holdings
					.Where(h => h.Activities.Any())
					.OrderByDescending(h => h.Activities.Count)
					.Take(10)
					.ToList();

				foreach (var holding in topHoldings)
				{
					var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
					var symbol = symbolProfile?.Symbol ?? "Unknown";

					logger.LogInformation("Analyzing holding: {Symbol}", symbol);

					if (!holding.Activities.Any())
					{
						logger.LogInformation("No activities found for {Symbol}", symbol);
						continue;
					}

					var startDate = holding.Activities.Min(a => a.Date);
					var endDate = holding.Activities.Max(a => a.Date);

					// Generate performance for this specific holding with storage
					var performance = await analysisService.CalculatePortfolioPerformanceAsync(
						new List<Holding> { holding }, startDate, endDate, baseCurrency, forceRecalculation: false);

					logger.LogInformation("{Symbol} Performance: TWR {TWR:F2}%, Dividends {Dividends}, Period {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
						symbol, performance.TimeWeightedReturn, performance.TotalDividends, startDate, endDate);

					// Calculate current value if market data is available
					if (symbolProfile?.MarketData != null && symbolProfile.MarketData.Any())
					{
						var currentValue = await marketDataCalculator.CalculateAccuratePortfolioValueAsync(
							new List<Holding> { holding }, DateTime.Now, baseCurrency);
						
						var quantity = marketDataCalculator.CalculateQuantityAtDate(holding.Activities, DateTime.Now);

						logger.LogInformation("  Current Position: {Quantity:F4} shares, Value: {Value}",
							quantity, currentValue);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating detailed holding analysis");
			}
		}

		/// <summary>
		/// Display storage statistics and available periods for monitoring
		/// </summary>
		private async Task DisplayStorageStatistics()
		{
			try
			{
				logger.LogInformation("=== Performance Storage Statistics ===");

				var stats = await analysisService.GetStorageStatisticsAsync();
				
				logger.LogInformation("Total Performance Snapshots: {TotalSnapshots}", stats.TotalSnapshots);
				logger.LogInformation("Latest Snapshots: {LatestSnapshots}", stats.LatestSnapshots);
				logger.LogInformation("Historical Snapshots: {HistoricalSnapshots}", stats.HistoricalSnapshots);
				
				if (stats.OldestSnapshot.HasValue)
				{
					logger.LogInformation("Oldest Snapshot: {OldestSnapshot:yyyy-MM-dd HH:mm:ss}", stats.OldestSnapshot.Value);
				}
				
				if (stats.NewestSnapshot.HasValue)
				{
					logger.LogInformation("Newest Snapshot: {NewestSnapshot:yyyy-MM-dd HH:mm:ss}", stats.NewestSnapshot.Value);
				}

				// Display snapshots by calculation type
				foreach (var (calcType, count) in stats.SnapshotsByCalculationType)
				{
					logger.LogInformation("Calculation Type {CalculationType}: {Count} snapshots", calcType, count);
				}

				// Display available periods
				logger.LogInformation("=== Available Performance Periods ===");
				var availablePeriods = await analysisService.GetAvailablePeriodsAsync();
				
				foreach (var (startDate, endDate, currency, calcType) in availablePeriods.Take(10)) // Show first 10
				{
					logger.LogInformation("Period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} ({Currency}, {CalculationType})",
						startDate, endDate, currency.Symbol, calcType);
				}

				if (availablePeriods.Count > 10)
				{
					logger.LogInformation("... and {AdditionalCount} more periods", availablePeriods.Count - 10);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error displaying storage statistics");
			}
		}
	}
}