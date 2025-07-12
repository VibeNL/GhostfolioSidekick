using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Analysis
{
	public class PortfolioPerformanceAnalysisTask(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		PortfolioAnalysisService portfolioAnalysisService,
		MarketDataPortfolioPerformanceCalculator marketDataCalculator,
		ILogger<PortfolioPerformanceAnalysisTask> logger) : IScheduledWork
	{
		public string Name => "Portfolio Performance Analysis";

		public TaskPriority Priority => TaskPriority.DisplayInformation;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			logger.LogInformation("Starting market data-driven portfolio performance analysis task");

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

				// Generate comprehensive portfolio insights
				await portfolioAnalysisService.GeneratePortfolioInsightsAsync(holdings, Currency.EUR);

				// Analyze performance for different time periods
				await AnalyzeMultiplePeriodsAsync(holdings);

				// Compare different periods using market data
				await ComparePerformancePeriodsAsync(holdings);

				// Generate individual holding reports with market data
				await GenerateAccurateHoldingReportsAsync(holdings, Currency.EUR);

				// Generate comprehensive market data-driven report
				await GenerateMarketDataDrivenReportAsync(holdings, Currency.EUR);

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

		private async Task AnalyzeMultiplePeriodsAsync(List<Holding> holdings)
		{
			var now = DateTime.Now;
			var baseCurrency = Currency.EUR; // Could be configurable

			// Analyze different time periods
			var periods = new[]
			{
				("Last Month", now.AddMonths(-1), now),
				("Last Quarter", now.AddMonths(-3), now),
				("Last 6 Months", now.AddMonths(-6), now),
				("Last Year", now.AddYears(-1), now),
				("Year to Date", new DateTime(now.Year, 1, 1), now)
			};

			foreach (var (periodName, startDate, endDate) in periods)
			{
				logger.LogInformation("Analyzing performance for period: {PeriodName}", periodName);

				try
				{
					var hasActivities = holdings
						.SelectMany(h => h.Activities)
						.Any(a => a.Date >= startDate && a.Date <= endDate);

					if (!hasActivities)
					{
						logger.LogInformation("No activities found for {PeriodName}, skipping", periodName);
						continue;
					}

					logger.LogInformation("=== {PeriodName} Performance ===", periodName);
					
					 // Use market data-driven analysis for maximum accuracy
					await portfolioAnalysisService.AnalyzePortfolioPerformanceAsync(
						holdings, startDate, endDate, baseCurrency);

					logger.LogInformation(""); // Empty line for readability
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to analyze performance for {PeriodName}", periodName);
				}
			}
		}

		private async Task ComparePerformancePeriodsAsync(List<Holding> holdings)
		{
			logger.LogInformation("Comparing performance across different periods using market data");

			var now = DateTime.Now;
			var baseCurrency = Currency.EUR;

			var comparisonPeriods = new List<(string Name, DateTime Start, DateTime End)>
			{
				("Q1 Current Year", new DateTime(now.Year, 1, 1), new DateTime(now.Year, 3, 31)),
				("Q2 Current Year", new DateTime(now.Year, 4, 1), new DateTime(now.Year, 6, 30)),
				("Q3 Current Year", new DateTime(now.Year, 7, 1), new DateTime(now.Year, 9, 30)),
				("Q4 Current Year", new DateTime(now.Year, 10, 1), new DateTime(now.Year, 12, 31)),
				("Last 12 Months", now.AddYears(-1), now),
				("Last 6 Months", now.AddMonths(-6), now),
				("Last 3 Months", now.AddMonths(-3), now)
			};

			try
			{
				await portfolioAnalysisService.ComparePerformancePeriodsAsync(holdings, comparisonPeriods, baseCurrency);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error during performance period comparison");
			}
		}

		/// <summary>
		/// Generate accurate performance reports for specific holdings using market data
		/// </summary>
		public async Task GenerateAccurateHoldingReportsAsync(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("Generating accurate individual holding reports using market data");

			foreach (var holding in holdings.Take(10)) // Limit to first 10 holdings for demo
			{
				try
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

					// Generate accurate summary using market data
					var summary = await portfolioAnalysisService.GenerateAccuratePerformanceSummaryAsync(
						new List<Holding> { holding }, startDate, endDate, baseCurrency);
					
					logger.LogInformation("Performance for {Symbol}:\n{Summary}", symbol, summary);

					// Generate detailed valuation report if market data is available
					if (symbolProfile?.MarketData != null && symbolProfile.MarketData.Any())
					{
						var currentValue = await marketDataCalculator.CalculateAccuratePortfolioValueAsync(
							new List<Holding> { holding }, DateTime.Now, baseCurrency);
						
						var quantity = holding.Activities
							.OfType<Model.Activities.Types.BuySellActivity>()
							.Sum(a => a.Quantity);

						logger.LogInformation("Current holding details:");
						logger.LogInformation("  Quantity: {Quantity:F4}", quantity);
						logger.LogInformation("  Current Value: {Value}", currentValue);
						logger.LogInformation("  Market Data Points: {DataPoints}", symbolProfile.MarketData.Count);
					}
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to generate report for holding");
				}
			}
		}

		/// <summary>
		/// Generate a comprehensive market data-driven portfolio report
		/// </summary>
		public async Task GenerateMarketDataDrivenReportAsync(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("=== Comprehensive Market Data-Driven Portfolio Report ===");

			try
			{
				// Overall portfolio statistics
				var totalHoldings = holdings.Count;
				var totalActivities = holdings.SelectMany(h => h.Activities).Count();
				var dateRange = holdings.SelectMany(h => h.Activities).Any() 
					? $"{holdings.SelectMany(h => h.Activities).Min(a => a.Date):yyyy-MM-dd} to {holdings.SelectMany(h => h.Activities).Max(a => a.Date):yyyy-MM-dd}"
					: "No activities";

				logger.LogInformation("Portfolio Overview:");
				logger.LogInformation("  Total Holdings: {TotalHoldings}", totalHoldings);
				logger.LogInformation("  Total Activities: {TotalActivities}", totalActivities);
				logger.LogInformation("  Date Range: {DateRange}", dateRange);
				logger.LogInformation("  Base Currency: {BaseCurrency}", baseCurrency.Symbol);
				logger.LogInformation("");

				// Current accurate portfolio value
				var currentValue = await marketDataCalculator.CalculateAccuratePortfolioValueAsync(
					holdings, DateTime.Now, baseCurrency);
				logger.LogInformation("Current Portfolio Value (Market Data): {CurrentValue}", currentValue);

				// Recent performance (last 3 months) with market data accuracy
				var recentStart = DateTime.Now.AddMonths(-3);
				var recentEnd = DateTime.Now;

				logger.LogInformation("=== Recent Performance Analysis (Market Data-Driven) ===");
				await portfolioAnalysisService.AnalyzePortfolioPerformanceAsync(
					holdings, recentStart, recentEnd, baseCurrency);

				// Year-to-date performance
				var ytdStart = new DateTime(DateTime.Now.Year, 1, 1);
				logger.LogInformation("=== Year-to-Date Performance Analysis ===");
				await portfolioAnalysisService.AnalyzePortfolioPerformanceAsync(
					holdings, ytdStart, DateTime.Now, baseCurrency);

				// Portfolio allocation analysis
				logger.LogInformation("=== Portfolio Allocation Analysis ===");
				await AnalyzePortfolioAllocation(holdings, baseCurrency);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error generating comprehensive report");
			}
		}

		/// <summary>
		/// Analyze portfolio allocation by asset class and currency
		/// </summary>
		private async Task AnalyzePortfolioAllocation(List<Holding> holdings, Currency baseCurrency)
		{
			try
			{
				var totalValue = await marketDataCalculator.CalculateAccuratePortfolioValueAsync(
					holdings, DateTime.Now, baseCurrency);

				if (totalValue.Amount == 0)
				{
					logger.LogInformation("Portfolio value is zero, skipping allocation analysis");
					return;
				}

				// Asset class allocation
				var assetClassAllocation = new Dictionary<string, decimal>();
				var currencyAllocation = new Dictionary<string, decimal>();

				foreach (var holding in holdings)
				{
					var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
					if (symbolProfile == null) continue;

					var holdingValue = await marketDataCalculator.CalculateHoldingValueAsync(holding, DateTime.Now, baseCurrency);
					var percentage = (holdingValue.Amount / totalValue.Amount) * 100;

					// Asset class allocation
					var assetClass = symbolProfile.AssetClass.ToString();
					if (!assetClassAllocation.ContainsKey(assetClass))
						assetClassAllocation[assetClass] = 0;
					assetClassAllocation[assetClass] += percentage;

					// Currency allocation  
					var currency = symbolProfile.Currency?.Symbol ?? "Unknown";
					if (!currencyAllocation.ContainsKey(currency))
						currencyAllocation[currency] = 0;
					currencyAllocation[currency] += percentage;
				}

				logger.LogInformation("Asset Class Allocation:");
				foreach (var kvp in assetClassAllocation.OrderByDescending(x => x.Value))
				{
					logger.LogInformation("  {AssetClass}: {Percentage:F1}%", kvp.Key, kvp.Value);
				}

				logger.LogInformation("Currency Allocation:");
				foreach (var kvp in currencyAllocation.OrderByDescending(x => x.Value))
				{
					logger.LogInformation("  {Currency}: {Percentage:F1}%", kvp.Key, kvp.Value);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error analyzing portfolio allocation");
			}
		}
	}
}