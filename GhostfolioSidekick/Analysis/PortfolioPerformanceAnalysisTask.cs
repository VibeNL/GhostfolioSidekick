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
		ILogger<PortfolioPerformanceAnalysisTask> logger) : IScheduledWork
	{
		public string Name => "Portfolio Performance Analysis";

		public TaskPriority Priority => TaskPriority.PerformanceCalculator;

		public TimeSpan ExecutionFrequency => Frequencies.Daily;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			logger.LogInformation("Starting enhanced portfolio performance analysis with per-asset and per-account breakdown");

			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync();

				// Get all holdings with their activities and market data
				var holdings = await context.Holdings
					.Include(h => h.Activities)
					.ThenInclude(a => a.Account)
					.Include(h => h.SymbolProfiles)
					.ThenInclude(sp => sp.MarketData)
					.AsSplitQuery()
					.ToListAsync();

				if (!holdings.Any())
				{
					logger.LogWarning("No holdings found for performance analysis");
					return;
				}

				logger.LogInformation("Found {HoldingCount} holdings for analysis", holdings.Count);

				// Check market data quality
				await AssessMarketDataQuality(holdings);

				// Show dynamic time periods calculation
				await DisplayDynamicTimePeriodsReport(holdings);

				// Generate comprehensive performance report (portfolio + accounts + assets)
				await GenerateComprehensivePerformanceReport(holdings, Currency.EUR);

				// Generate account-specific analysis
				await GenerateAccountPerformanceAnalysis(holdings, Currency.EUR);

				// Generate asset-specific analysis
				await GenerateAssetPerformanceAnalysis(holdings, Currency.EUR);

				// Display storage statistics
				await DisplayStorageStatistics();

				// Clean up old performance calculation snapshots
				await CleanupOldPerformanceSnapshots();

				logger.LogInformation("Portfolio performance analysis completed successfully");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during portfolio performance analysis");
				throw new InvalidOperationException("Portfolio performance analysis failed. See inner exception for details.", ex);
			}
		}

		/// <summary>
		 /// Generate comprehensive performance report with portfolio, account, and asset breakdowns
		 /// </summary>
		private async Task GenerateComprehensivePerformanceReport(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("=== Comprehensive Performance Report ===");

			try
			{
				// Get all meaningful time periods for comprehensive analysis
				var allPeriods = analysisService.CalculateAllMeaningfulTimePeriods(holdings);
				
				if (!allPeriods.Any())
				{
					logger.LogWarning("No meaningful time periods found for comprehensive analysis");
					return;
				}

				logger.LogInformation("Analyzing {PeriodCount} time periods", allPeriods.Count);

				// Analyze the most recent quarter for detailed breakdown
				var recentPeriod = allPeriods.FirstOrDefault(p => 
					p.Name.Contains("Last Quarter") || p.Name.Contains("Q") || p.Name.Contains("Month"));

				if (recentPeriod.Name != null)
				{
					logger.LogInformation("Detailed analysis for period: {PeriodName} ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd})", 
						recentPeriod.Name, recentPeriod.Start, recentPeriod.End);

					var report = await analysisService.GenerateComprehensivePerformanceReportAsync(
						holdings, recentPeriod.Start, recentPeriod.End, baseCurrency, forceRecalculation: false);

					logger.LogInformation("Generated at: {GeneratedAt:yyyy-MM-dd HH:mm:ss}", report.GeneratedAt);
					logger.LogInformation("");

					// Portfolio-wide performance
					logger.LogInformation("=== Portfolio Performance ===");
					var portfolio = report.PortfolioPerformance;
					logger.LogInformation("Time-Weighted Return: {TWR:F2}%", portfolio.TimeWeightedReturn);
					logger.LogInformation("Portfolio Value: {InitialValue} ? {FinalValue}", 
						portfolio.InitialValue, portfolio.FinalValue);
					logger.LogInformation("Total Dividends: {Dividends}", portfolio.TotalDividends);
					logger.LogInformation("");

					// Account performance summary
					logger.LogInformation("=== Account Performance Summary ===");
					logger.LogInformation("Number of Accounts: {AccountCount}", report.AccountPerformances.Count);
					if (report.AccountPerformances.Any())
					{
						logger.LogInformation("Average Account TWR: {AvgTWR:F2}%", report.Summary.AverageAccountTWR);
						logger.LogInformation("Best Performing Account: {Account} ({TWR:F2}%)", 
							report.Summary.BestPerformingAccount.Key, 
							report.Summary.BestPerformingAccount.Value.TimeWeightedReturn);
						logger.LogInformation("Worst Performing Account: {Account} ({TWR:F2}%)", 
							report.Summary.WorstPerformingAccount.Key, 
							report.Summary.WorstPerformingAccount.Value.TimeWeightedReturn);
					}
					logger.LogInformation("");

					// Asset performance summary
					logger.LogInformation("=== Asset Performance Summary ===");
					logger.LogInformation("Number of Assets: {AssetCount}", report.AssetPerformances.Count);
					if (report.AssetPerformances.Any())
					{
						logger.LogInformation("Average Asset TWR: {AvgTWR:F2}%", report.Summary.AverageAssetTWR);
						logger.LogInformation("Best Performing Asset: {Asset} ({TWR:F2}%)", 
							report.Summary.BestPerformingAsset.Key, 
							report.Summary.BestPerformingAsset.Value.TimeWeightedReturn);
						logger.LogInformation("Worst Performing Asset: {Asset} ({TWR:F2}%)", 
							report.Summary.WorstPerformingAsset.Key, 
							report.Summary.WorstPerformingAsset.Value.TimeWeightedReturn);
					}
				}

				// Generate performance overview for all calculated periods
				await GenerateAllPeriodsPerformanceOverview(holdings, baseCurrency);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating comprehensive performance report");
			}
		}

		/// <summary>
		/// Generate performance overview for all calculated time periods
		/// </summary>
		private async Task GenerateAllPeriodsPerformanceOverview(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("=== All Time Periods Performance Overview ===");

			try
			{
				var allPeriodsPerformance = await analysisService.GetComprehensiveTimePeriodsAnalysisAsync(
					holdings, baseCurrency, forceRecalculation: false);

				if (!allPeriodsPerformance.Any())
				{
					logger.LogInformation("No performance data available for any time period");
					return;
				}

				logger.LogInformation("Performance overview for {PeriodCount} time periods:", allPeriodsPerformance.Count);
				logger.LogInformation("");

				// Group and display by period type
				var groupedPeriods = allPeriodsPerformance
					.GroupBy(kvp => GetPeriodCategory(kvp.Key))
					.OrderBy(g => GetCategoryOrder(g.Key));

				foreach (var group in groupedPeriods)
				{
					logger.LogInformation("=== {CategoryName} ===", group.Key);
					
					var sortedPeriods = group.OrderByDescending(kvp => kvp.Value.EndDate).Take(10); // Show most recent 10 per category
					
					foreach (var (periodName, performance) in sortedPeriods)
					{
						var days = (performance.EndDate - performance.StartDate).TotalDays;
						var annualizedReturn = CalculateAnnualizedReturn(performance.TimeWeightedReturn, days);
						
						logger.LogInformation("{Period}: TWR {TWR:F2}% | Annualized {Annualized:F2}% | Value {FinalValue} | Days: {Days:F0}",
							periodName,
							performance.TimeWeightedReturn,
							annualizedReturn,
							performance.FinalValue,
							days);
					}
					logger.LogInformation("");
				}

				// Performance statistics
				var allTWRs = allPeriodsPerformance.Values.Select(p => p.TimeWeightedReturn).ToList();
				if (allTWRs.Any())
				{
					logger.LogInformation("=== Performance Statistics Across All Periods ===");
					logger.LogInformation("Best Period Performance: {Best:F2}%", allTWRs.Max());
					logger.LogInformation("Worst Period Performance: {Worst:F2}%", allTWRs.Min());
					logger.LogInformation("Average Performance: {Average:F2}%", allTWRs.Average());
					logger.LogInformation("Median Performance: {Median:F2}%", CalculateMedian(allTWRs));
					
					var positiveCount = allTWRs.Count(twr => twr > 0);
					var positivePercentage = (double)positiveCount / allTWRs.Count * 100;
					logger.LogInformation("Positive Periods: {PositiveCount}/{TotalCount} ({Percentage:F1}%)", 
						positiveCount, allTWRs.Count, positivePercentage);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating all periods performance overview");
			}
		}

		/// <summary>
		/// Categorize periods for better organization
		/// </summary>
		private string GetPeriodCategory(string periodName)
		{
			if (periodName.Contains("Week")) return "Weekly Periods";
			if (periodName.Contains("Month") && !periodName.Contains("Year")) return "Monthly Periods";
			if (periodName.Contains("Quarter") || periodName.StartsWith("Q")) return "Quarterly Periods";
			if (periodName.Contains("Year") && !periodName.Contains("Rolling")) return "Annual Periods";
			if (periodName.Contains("Rolling")) return "Rolling Periods";
			if (periodName.Contains("Inception")) return "Inception Periods";
			if (periodName.Contains("First")) return "Milestone Periods";
			return "Other Periods";
		}

		/// <summary>
		/// Get category display order
		/// </summary>
		private int GetCategoryOrder(string category)
		{
			return category switch
			{
				"Weekly Periods" => 1,
				"Monthly Periods" => 2,
				"Quarterly Periods" => 3,
				"Annual Periods" => 4,
				"Rolling Periods" => 5,
				"Milestone Periods" => 6,
				"Inception Periods" => 7,
				_ => 8
			};
		}

		/// <summary>
		/// Calculate annualized return from time-weighted return and period length
		/// </summary>
		private decimal CalculateAnnualizedReturn(decimal timeWeightedReturn, double days)
		{
			if (days <= 0) return timeWeightedReturn;
			
			var years = days / 365.25;
			if (years < 0.1) return timeWeightedReturn; // For very short periods, return as-is
			
			return (decimal)(Math.Pow((double)(1 + timeWeightedReturn / 100), 1 / years) - 1) * 100;
		}

		/// <summary>
		/// Generate detailed account performance analysis
		/// </summary>
		private async Task GenerateAccountPerformanceAnalysis(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("=== Account Performance Analysis ===");

			try
			{
				// Get meaningful time periods and select a representative period for detailed analysis
				var allPeriods = analysisService.CalculateAllMeaningfulTimePeriods(holdings);
				
				// Use the longest meaningful period for comprehensive account analysis
				var analysisPeriod = allPeriods
					.Where(p => !p.Name.Contains("Rolling") && !p.Name.Contains("Week"))
					.OrderByDescending(p => (p.End - p.Start).TotalDays)
					.FirstOrDefault();

				if (analysisPeriod.Name == null)
				{
					logger.LogInformation("No suitable period found for account analysis");
					return;
				}

				logger.LogInformation("Account analysis period: {PeriodName} ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd})", 
					analysisPeriod.Name, analysisPeriod.Start, analysisPeriod.End);

				var accountPerformances = await analysisService.CalculateAllAccountsPerformanceAsync(
					holdings, analysisPeriod.Start, analysisPeriod.End, baseCurrency, forceRecalculation: false);

				if (!accountPerformances.Any())
				{
					logger.LogInformation("No account performances found for the period");
					return;
				}

				// Sort accounts by performance
				var sortedAccounts = accountPerformances
					.OrderByDescending(kvp => kvp.Value.TimeWeightedReturn)
					.ToList();

				foreach (var (accountName, performance) in sortedAccounts)
				{
					var days = (performance.EndDate - performance.StartDate).TotalDays;
					var annualizedReturn = CalculateAnnualizedReturn(performance.TimeWeightedReturn, days);
					
					logger.LogInformation("Account: {AccountName}", accountName);
					logger.LogInformation("  TWR: {TWR:F2}% | Annualized: {Annualized:F2}%", performance.TimeWeightedReturn, annualizedReturn);
					logger.LogInformation("  Value Change: {InitialValue} ? {FinalValue}", 
						performance.InitialValue, performance.FinalValue);
					logger.LogInformation("  Dividends: {Dividends}", performance.TotalDividends);
					logger.LogInformation("  Net Cash Flows: {NetCashFlows}", performance.NetCashFlows);
					logger.LogInformation("");
				}

				// Calculate account allocation
				var totalPortfolioValue = accountPerformances.Values.Sum(p => p.FinalValue.Amount);
				if (totalPortfolioValue > 0)
				{
					logger.LogInformation("=== Account Allocation ===");
					foreach (var (accountName, performance) in sortedAccounts)
					{
						var allocation = (performance.FinalValue.Amount / totalPortfolioValue) * 100;
						logger.LogInformation("{AccountName}: {Allocation:F1}% ({Value})", 
							accountName, allocation, performance.FinalValue);
					}
				}

				// Generate account performance across multiple periods for trend analysis
				await GenerateAccountPerformanceTrends(holdings, baseCurrency, sortedAccounts.Take(5).Select(kvp => kvp.Key).ToList());
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating account performance analysis");
			}
		}

		/// <summary>
		/// Generate account performance trends across multiple time periods
		/// </summary>
		private async Task GenerateAccountPerformanceTrends(List<Holding> holdings, Currency baseCurrency, List<string> topAccounts)
		{
			if (!topAccounts.Any()) return;

			logger.LogInformation("=== Account Performance Trends (Top {AccountCount} Accounts) ===", topAccounts.Count);

			try
			{
				// Get key periods for trend analysis
				var allPeriods = analysisService.CalculateAllMeaningfulTimePeriods(holdings);
				var trendPeriods = allPeriods
					.Where(p => p.Name.Contains("Year") && !p.Name.Contains("Rolling") || 
					           p.Name.Contains("Quarter") || 
					           p.Name.Contains("Inception"))
					.OrderByDescending(p => p.End)
					.Take(8)
					.ToList();

				foreach (var account in topAccounts)
				{
					logger.LogInformation("Account: {AccountName}", account);
					
					foreach (var period in trendPeriods)
					{
						try
						{
							var performance = await analysisService.CalculateAccountPerformanceAsync(
								holdings, account, period.Start, period.End, baseCurrency, forceRecalculation: false);
							
							var days = (performance.EndDate - performance.StartDate).TotalDays;
							var annualizedReturn = CalculateAnnualizedReturn(performance.TimeWeightedReturn, days);
							
							logger.LogInformation("  {Period}: {TWR:F2}% (Annualized: {Annualized:F2}%)",
								period.Name, performance.TimeWeightedReturn, annualizedReturn);
						}
						catch (Exception ex)
						{
							logger.LogDebug(ex, "Could not calculate {Account} performance for {Period}", account, period.Name);
						}
					}
					logger.LogInformation("");
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating account performance trends");
			}
		}

		/// <summary>
		/// Generate detailed asset performance analysis
		/// </summary>
		private async Task GenerateAssetPerformanceAnalysis(List<Holding> holdings, Currency baseCurrency)
		{
			logger.LogInformation("=== Asset Performance Analysis ===");

			try
			{
				// Get meaningful time periods and select a representative period for detailed analysis
				var allPeriods = analysisService.CalculateAllMeaningfulTimePeriods(holdings);
				
				// Use a substantial period for asset analysis (prefer last year or last quarter)
				var analysisPeriod = allPeriods
					.Where(p => p.Name.Contains("Year") || p.Name.Contains("Quarter") || p.Name.Contains("Month"))
					.Where(p => !p.Name.Contains("Rolling"))
					.OrderByDescending(p => p.End)
					.ThenByDescending(p => (p.End - p.Start).TotalDays)
					.FirstOrDefault();

				if (analysisPeriod.Name == null)
				{
					logger.LogInformation("No suitable period found for asset analysis");
					return;
				}

				logger.LogInformation("Asset analysis period: {PeriodName} ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd})", 
					analysisPeriod.Name, analysisPeriod.Start, analysisPeriod.End);

				var assetPerformances = await analysisService.CalculateAllAssetsPerformanceAsync(
					holdings, analysisPeriod.Start, analysisPeriod.End, baseCurrency, forceRecalculation: false);

				if (!assetPerformances.Any())
				{
					logger.LogInformation("No asset performances found for the period");
					return;
				}

				// Sort assets by performance
				var sortedAssets = assetPerformances
					.OrderByDescending(kvp => kvp.Value.TimeWeightedReturn)
					.Take(20) // Show top 20 performers
					.ToList();

				foreach (var (symbol, performance) in sortedAssets)
				{
					var days = (performance.EndDate - performance.StartDate).TotalDays;
					var annualizedReturn = CalculateAnnualizedReturn(performance.TimeWeightedReturn, days);
					
					logger.LogInformation("Asset: {Symbol}", symbol);
					logger.LogInformation("  TWR: {TWR:F2}% | Annualized: {Annualized:F2}%", performance.TimeWeightedReturn, annualizedReturn);
					logger.LogInformation("  Value Change: {InitialValue} ? {FinalValue}", 
						performance.InitialValue, performance.FinalValue);
					logger.LogInformation("  Dividends: {Dividends}", performance.TotalDividends);
					logger.LogInformation("  Currency Impact: {CurrencyImpact:F2}%", performance.CurrencyImpact);
					logger.LogInformation("");
				}

				// Show asset allocation for top performers
				var totalPortfolioValue = assetPerformances.Values.Sum(p => p.FinalValue.Amount);
				if (totalPortfolioValue > 0)
				{
					logger.LogInformation("=== Top Asset Allocation ===");
					foreach (var (symbol, performance) in sortedAssets.Take(10))
					{
						var allocation = (performance.FinalValue.Amount / totalPortfolioValue) * 100;
						logger.LogInformation("{Symbol}: {Allocation:F1}% ({Value})", 
							symbol, allocation, performance.FinalValue);
					}
				}

				// Performance distribution analysis
				var allTWRs = assetPerformances.Values.Select(p => p.TimeWeightedReturn).ToList();
				if (allTWRs.Any())
				{
					logger.LogInformation("=== Asset Performance Distribution ===");
					logger.LogInformation("Best Performer: {Best:F2}%", allTWRs.Max());
					logger.LogInformation("Worst Performer: {Worst:F2}%", allTWRs.Min());
					logger.LogInformation("Average: {Average:F2}%", allTWRs.Average());
					logger.LogInformation("Median: {Median:F2}%", CalculateMedian(allTWRs));
					
					var positivePerformers = allTWRs.Count(twr => twr > 0);
					var positivePercentage = (double)positivePerformers / allTWRs.Count * 100;
					logger.LogInformation("Positive Performers: {PositiveCount}/{TotalCount} ({Percentage:F1}%)", 
						positivePerformers, allTWRs.Count, positivePercentage);
				}

				// Generate asset performance across multiple periods for top performers
				await GenerateAssetPerformanceTrends(holdings, baseCurrency, sortedAssets.Take(10).Select(kvp => kvp.Key).ToList());
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating asset performance analysis");
			}
		}

		/// <summary>
		/// Generate asset performance trends across multiple time periods
		/// </summary>
		private async Task GenerateAssetPerformanceTrends(List<Holding> holdings, Currency baseCurrency, List<string> topAssets)
		{
			if (!topAssets.Any()) return;

			logger.LogInformation("=== Asset Performance Trends (Top {AssetCount} Assets) ===", topAssets.Count);

			try
			{
				// Get key periods for trend analysis
				var allPeriods = analysisService.CalculateAllMeaningfulTimePeriods(holdings);
				var trendPeriods = allPeriods
					.Where(p => p.Name.Contains("Year") && !p.Name.Contains("Rolling") || 
					           p.Name.Contains("Quarter") || 
					           p.Name.Contains("Inception"))
					.OrderByDescending(p => p.End)
					.Take(6)
					.ToList();

				foreach (var asset in topAssets)
				{
					logger.LogInformation("Asset: {Symbol}", asset);
					
					foreach (var period in trendPeriods)
					{
						try
						{
							var performance = await analysisService.CalculateAssetPerformanceAsync(
								holdings, asset, period.Start, period.End, baseCurrency, forceRecalculation: false);
							
							var days = (performance.EndDate - performance.StartDate).TotalDays;
							var annualizedReturn = CalculateAnnualizedReturn(performance.TimeWeightedReturn, days);
							
							logger.LogInformation("  {Period}: {TWR:F2}% (Annualized: {Annualized:F2}%)",
								period.Name, performance.TimeWeightedReturn, annualizedReturn);
						}
						catch (Exception ex)
						{
							logger.LogDebug(ex, "Could not calculate {Asset} performance for {Period}", asset, period.Name);
						}
					}
					logger.LogInformation("");
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating asset performance trends");
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

			await Task.CompletedTask;
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

				// Display snapshots by scope
				foreach (var (scope, count) in stats.SnapshotsByScope)
				{
					logger.LogInformation("Scope {Scope}: {Count} snapshots", scope, count);
				}

				// Display sample of available periods
				logger.LogInformation("=== Sample Available Performance Periods ===");
				var availablePeriods = await analysisService.GetAvailablePeriodsAsync();
				
				var samplePeriods = availablePeriods.Take(15).ToList(); // Show first 15
				foreach (var (startDate, endDate, currency, calcType, scope, scopeId) in samplePeriods)
				{
					logger.LogInformation("Period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} ({Currency}, {CalculationType}, {Scope}:{ScopeId})",
						startDate, endDate, currency.Symbol, calcType, scope, scopeId ?? "All");
				}

				if (availablePeriods.Count > 15)
				{
					logger.LogInformation("... and {AdditionalCount} more periods", availablePeriods.Count - 15);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error displaying storage statistics");
			}
		}

		/// <summary>
		/// Calculate median value from a list of decimals
		/// </summary>
		private decimal CalculateMedian(List<decimal> values)
		{
			if (!values.Any()) return 0;

			var sorted = values.OrderBy(x => x).ToList();
			var count = sorted.Count;

			if (count % 2 == 0)
			{
				return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
			}
			else
			{
				return sorted[count / 2];
			}
		}

		/// <summary>
		/// Clean up old performance calculation snapshots to prevent database bloat
		/// </summary>
		private async Task CleanupOldPerformanceSnapshots()
		{
			try
			{
				logger.LogInformation("=== Performance Snapshots Cleanup ===");

				// Get current storage statistics before cleanup
				var statsBefore = await analysisService.GetStorageStatisticsAsync();
				logger.LogInformation("Before cleanup: {TotalSnapshots} total snapshots ({LatestSnapshots} latest, {HistoricalSnapshots} historical)",
					statsBefore.TotalSnapshots, statsBefore.LatestSnapshots, statsBefore.HistoricalSnapshots);

				// Clean up old versions, keeping only the latest 3 versions for each period/scope combination
				const int versionsToKeep = 3;
				await analysisService.CleanupOldSnapshotsAsync(versionsToKeep);

				// Get storage statistics after cleanup to show the impact
				var statsAfter = await analysisService.GetStorageStatisticsAsync();
				var snapshotsRemoved = statsBefore.TotalSnapshots - statsAfter.TotalSnapshots;

				if (snapshotsRemoved > 0)
				{
					logger.LogInformation("Cleanup completed: Removed {RemovedCount} old performance snapshots", snapshotsRemoved);
					logger.LogInformation("After cleanup: {TotalSnapshots} total snapshots ({LatestSnapshots} latest, {HistoricalSnapshots} historical)",
						statsAfter.TotalSnapshots, statsAfter.LatestSnapshots, statsAfter.HistoricalSnapshots);
				}
				else
				{
					logger.LogInformation("No old snapshots found for cleanup");
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error during performance snapshots cleanup");
			}
		}

		/// <summary>
		/// Display dynamic time periods calculation report
		/// </summary>
		private async Task DisplayDynamicTimePeriodsReport(List<Holding> holdings)
		{
			try
			{
				logger.LogInformation("=== Dynamic Time Periods Calculation ===");

				var report = analysisService.GenerateTimePeriodsReport(holdings);
				
				// Log the report line by line for better formatting
				var lines = report.Split('\n', StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					logger.LogInformation(line.TrimEnd('\r'));
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error generating dynamic time periods report");
			}
			
			await Task.CompletedTask;
		}
	}
}