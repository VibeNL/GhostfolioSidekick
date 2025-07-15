using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Portfolio;
using GhostfolioSidekick.Model.Services;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Enhanced portfolio analysis service with persistent performance storage and per-asset/account analysis
	/// </summary>
	public class PortfolioAnalysisService
	{
		private readonly EnhancedPortfolioPerformanceCalculator enhancedCalculator;
		private readonly MarketDataPortfolioPerformanceCalculator marketDataCalculator;
		private readonly PortfolioPerformanceCalculator basicCalculator;
		private readonly PortfolioPerformanceStorageService storageService;
		private readonly ILogger<PortfolioAnalysisService> logger;

		public PortfolioAnalysisService(
			EnhancedPortfolioPerformanceCalculator enhancedCalculator,
			MarketDataPortfolioPerformanceCalculator marketDataCalculator,
			PortfolioPerformanceStorageService storageService,
			ILogger<PortfolioAnalysisService> logger)
		{
			this.enhancedCalculator = enhancedCalculator;
			this.marketDataCalculator = marketDataCalculator;
			this.basicCalculator = new PortfolioPerformanceCalculator();
			this.storageService = storageService;
			this.logger = logger;
		}

		#region Portfolio-wide Performance

		/// <summary>
		/// Calculate portfolio performance with persistent storage
		/// </summary>
		public async Task<PortfolioPerformance> CalculatePortfolioPerformanceAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			return await CalculatePerformanceWithScopeAsync(
				holdings, startDate, endDate, baseCurrency, 
				PerformanceScope.Portfolio, null, forceRecalculation);
		}

		/// <summary>
		/// Get portfolio performance with automatic period detection and storage
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> GetStandardPerformanceReportAsync(
			List<Holding> holdings,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			// Calculate all meaningful time periods dynamically
			var periods = CalculateAllMeaningfulTimePeriods(holdings);

			logger.LogInformation("Generating standard performance report for {PeriodCount} dynamically calculated periods",
				periods.Count);

			return await CalculateMultiplePeriodPerformanceAsync(holdings, periods, baseCurrency, forceRecalculation);
		}

		/// <summary>
		/// Calculate all meaningful time periods based on portfolio activity data
		/// </summary>
		public List<(string Name, DateTime Start, DateTime End)> CalculateAllMeaningfulTimePeriods(List<Holding> holdings)
		{
			var now = DateTime.Now;
			var periods = new List<(string Name, DateTime Start, DateTime End)>();

			// Get all activity dates to determine meaningful periods
			var allActivities = holdings.SelectMany(h => h.Activities).ToList();
			if (!allActivities.Any())
			{
				logger.LogWarning("No activities found for period calculation");
				return periods;
			}

			var firstActivity = allActivities.Min(a => a.Date);
			var lastActivity = allActivities.Max(a => a.Date);

			logger.LogInformation("Portfolio activity span: {FirstActivity:yyyy-MM-dd} to {LastActivity:yyyy-MM-dd}", 
				firstActivity, lastActivity);

			// Add different types of periods
			periods.AddRange(GetStandardPeriods(now, allActivities));
			periods.AddRange(GetYearlyPeriods(firstActivity, now, allActivities));
			periods.AddRange(GetQuarterlyPeriods(firstActivity, now, allActivities));
			periods.AddRange(GetMonthlyPeriods(now, allActivities));
			periods.AddRange(GetMilestonePeriods(firstActivity, now));
			periods.AddRange(GetInceptionPeriods(firstActivity, now));
			periods.AddRange(GetRollingPeriods(firstActivity, now, allActivities));

			// Remove duplicates and sort by end date descending (most recent first)
			var uniquePeriods = periods
				.GroupBy(p => new { p.Start, p.End })
				.Select(g => g.First())
				.OrderByDescending(p => p.End)
				.ThenByDescending(p => p.Start)
				.ToList();

			logger.LogInformation("Calculated {TotalPeriods} meaningful time periods from portfolio activities", 
				uniquePeriods.Count);

			return uniquePeriods;
		}

		/// <summary>
		/// Get standard relative periods
		/// </summary>
		private List<(string Name, DateTime Start, DateTime End)> GetStandardPeriods(DateTime now, List<Activity> allActivities)
		{
			var periods = new List<(string Name, DateTime Start, DateTime End)>();
			var standardPeriods = new List<(string Name, DateTime Start, DateTime End)>
			{
				("Last Week", now.AddDays(-7), now),
				("Last Month", now.AddMonths(-1), now),
				("Last Quarter", now.AddMonths(-3), now),
				("Last 6 Months", now.AddMonths(-6), now),
				("Last Year", now.AddYears(-1), now),
				("Year to Date", new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local), now),
				("Last 2 Years", now.AddYears(-2), now),
				("Last 3 Years", now.AddYears(-3), now)
			};

			foreach (var period in standardPeriods)
			{
				if (HasActivitiesInPeriod(allActivities, period.Start, period.End))
				{
					periods.Add(period);
				}
			}

			return periods;
		}

		/// <summary>
		/// Get yearly periods
		/// </summary>
		private List<(string Name, DateTime Start, DateTime End)> GetYearlyPeriods(DateTime firstActivity, DateTime now, List<Activity> allActivities)
		{
			var periods = new List<(string Name, DateTime Start, DateTime End)>();
			var firstYear = firstActivity.Year;
			var currentYear = now.Year;

			for (int year = firstYear; year <= currentYear; year++)
			{
				var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Local);
				var yearEnd = year == currentYear ? now : new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Local);

				if (HasActivitiesInPeriod(allActivities, yearStart, yearEnd))
				{
					periods.Add(($"Year {year}", yearStart, yearEnd));
				}
			}

			return periods;
		}

		/// <summary>
		/// Get quarterly periods
		/// </summary>
		private List<(string Name, DateTime Start, DateTime End)> GetQuarterlyPeriods(DateTime firstActivity, DateTime now, List<Activity> allActivities)
		{
			var periods = new List<(string Name, DateTime Start, DateTime End)>();
			var firstYear = firstActivity.Year;
			var currentYear = now.Year;

			for (int year = firstYear; year <= currentYear; year++)
			{
				for (int quarter = 1; quarter <= 4; quarter++)
				{
					var quarterStart = new DateTime(year, (quarter - 1) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Local);
					var quarterEnd = quarter == 4 && year == currentYear 
						? now 
						: quarterStart.AddMonths(3).AddDays(-1);

					// Don't add future quarters
					if (quarterStart > now) break;

					if (HasActivitiesInPeriod(allActivities, quarterStart, quarterEnd))
					{
						periods.Add(($"Q{quarter} {year}", quarterStart, quarterEnd));
					}
				}
			}

			return periods;
		}

		/// <summary>
		/// Get monthly periods for recent months
		/// </summary>
		private List<(string Name, DateTime Start, DateTime End)> GetMonthlyPeriods(DateTime now, List<Activity> allActivities)
		{
			var periods = new List<(string Name, DateTime Start, DateTime End)>();
			var monthStart = now.AddMonths(-24);

			while (monthStart < now)
			{
				var monthEnd = monthStart.AddMonths(1).AddDays(-1);
				if (monthEnd > now) monthEnd = now;

				if (HasActivitiesInPeriod(allActivities, monthStart, monthEnd))
				{
					periods.Add(($"{monthStart:MMMM yyyy}", monthStart, monthEnd));
				}

				monthStart = monthStart.AddMonths(1);
			}

			return periods;
		}

		/// <summary>
		/// Get milestone periods
		/// </summary>
		private List<(string Name, DateTime Start, DateTime End)> GetMilestonePeriods(DateTime firstActivity, DateTime now)
		{
			var periods = new List<(string Name, DateTime Start, DateTime End)>();
			var milestoneStart = firstActivity;
			var milestoneCounter = 1;

			while (milestoneStart.AddMonths(6) <= now)
			{
				var milestoneEnd = milestoneStart.AddMonths(6);
				if (milestoneEnd > now) milestoneEnd = now;

				periods.Add(($"First {milestoneCounter * 6} Months", milestoneStart, milestoneEnd));
				milestoneStart = milestoneStart.AddMonths(6);
				milestoneCounter++;
			}

			return periods;
		}

		/// <summary>
		/// Get inception periods
		/// </summary>
		private List<(string Name, DateTime Start, DateTime End)> GetInceptionPeriods(DateTime firstActivity, DateTime now)
		{
			var periods = new List<(string Name, DateTime Start, DateTime End)>();
			
			if (firstActivity < now)
			{
				periods.Add(("Inception to Date", firstActivity, now));
			}

			return periods;
		}

		/// <summary>
		/// Get rolling periods for portfolios with significant history
		/// </summary>
		private List<(string Name, DateTime Start, DateTime End)> GetRollingPeriods(DateTime firstActivity, DateTime now, List<Activity> allActivities)
		{
			var periods = new List<(string Name, DateTime Start, DateTime End)>();
			var portfolioAge = (now - firstActivity).TotalDays;
			
			if (portfolioAge <= 365) return periods; // Only for portfolios older than 1 year

			// Add rolling 1-year periods every 3 months
			var rollingStart = firstActivity;
			while (rollingStart.AddYears(1) <= now)
			{
				var rollingEnd = rollingStart.AddYears(1);
				if (HasActivitiesInPeriod(allActivities, rollingStart, rollingEnd))
				{
					periods.Add(($"Rolling Year {rollingStart:yyyy-MM-dd}", rollingStart, rollingEnd));
				}
				rollingStart = rollingStart.AddMonths(3);
			}

			return periods;
		}

		/// <summary>
		/// Check if there are any activities in the specified time period
		/// </summary>
		private bool HasActivitiesInPeriod(List<Activity> activities, DateTime start, DateTime end)
		{
			return activities.Any(a => a.Date >= start && a.Date <= end);
		}

		/// <summary>
		/// Get comprehensive performance analysis for all meaningful periods
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> GetComprehensiveTimePeriodsAnalysisAsync(
			List<Holding> holdings,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			var allPeriods = CalculateAllMeaningfulTimePeriods(holdings);
			
			logger.LogInformation("Running comprehensive analysis for {PeriodCount} time periods", allPeriods.Count);

			return await CalculateMultiplePeriodPerformanceAsync(holdings, allPeriods, baseCurrency, forceRecalculation);
		}

		#endregion

		#region Per-Account Performance

		/// <summary>
		/// Calculate performance for a specific account
		/// </summary>
		public async Task<PortfolioPerformance> CalculateAccountPerformanceAsync(
			List<Holding> holdings,
			string accountName,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			return await CalculatePerformanceWithScopeAsync(
				holdings, startDate, endDate, baseCurrency,
				PerformanceScope.Account, accountName, forceRecalculation);
		}

		/// <summary>
		/// Calculate performance for all accounts in the portfolio
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> CalculateAllAccountsPerformanceAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			// Get all unique account names
			var accountNames = holdings
				.SelectMany(h => h.Activities)
				.Where(a => a.Date >= startDate && a.Date <= endDate)
				.Select(a => a.Account?.Name)
				.Where(name => !string.IsNullOrEmpty(name))
				.Distinct()
				.ToList();

			logger.LogInformation("Calculating performance for {AccountCount} accounts", accountNames.Count);

			var results = new Dictionary<string, PortfolioPerformance>();

			foreach (var accountName in accountNames)
			{
				try
				{
					var performance = await CalculateAccountPerformanceAsync(
						holdings, accountName!, startDate, endDate, baseCurrency, forceRecalculation);
					
					results[accountName!] = performance;
					
					logger.LogDebug("Calculated performance for account {AccountName}: TWR {TWR:F2}%", 
						accountName, performance.TimeWeightedReturn);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to calculate performance for account {AccountName}", accountName);
				}
			}

			return results;
		}

		/// <summary>
		/// Get stored account performances from database
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> GetStoredAccountPerformancesAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType = "MarketData")
		{
			return await storageService.GetAccountPerformancesAsync(startDate, endDate, baseCurrency, calculationType);
		}

		#endregion

		#region Per-Asset Performance

		/// <summary>
		/// Calculate performance for a specific asset/symbol
		/// </summary>
		public async Task<PortfolioPerformance> CalculateAssetPerformanceAsync(
			List<Holding> holdings,
			string symbol,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			return await CalculatePerformanceWithScopeAsync(
				holdings, startDate, endDate, baseCurrency,
				PerformanceScope.Asset, symbol, forceRecalculation);
		}

		/// <summary>
		/// Calculate performance for all assets in the portfolio
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> CalculateAllAssetsPerformanceAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			// Get all unique symbols that have activities in the period
			var symbols = holdings
				.Where(h => h.Activities.Any(a => a.Date >= startDate && a.Date <= endDate))
				.SelectMany(h => h.SymbolProfiles)
				.Select(sp => sp.Symbol)
				.Distinct()
				.ToList();

			logger.LogInformation("Calculating performance for {AssetCount} assets", symbols.Count);

			var results = new Dictionary<string, PortfolioPerformance>();

			foreach (var symbol in symbols)
			{
				try
				{
					var performance = await CalculateAssetPerformanceAsync(
						holdings, symbol, startDate, endDate, baseCurrency, forceRecalculation);
					
					results[symbol] = performance;
					
					logger.LogDebug("Calculated performance for asset {Symbol}: TWR {TWR:F2}%", 
						symbol, performance.TimeWeightedReturn);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to calculate performance for asset {Symbol}", symbol);
				}
			}

			return results;
		}

		/// <summary>
		/// Get stored asset performances from database
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> GetStoredAssetPerformancesAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string calculationType = "MarketData")
		{
			return await storageService.GetAssetPerformancesAsync(startDate, endDate, baseCurrency, calculationType);
		}

		#endregion

		#region Multi-dimensional Analysis

		/// <summary>
		/// Generate comprehensive performance breakdown by portfolio, accounts, and assets
		/// </summary>
		public async Task<ComprehensivePerformanceReport> GenerateComprehensivePerformanceReportAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			logger.LogInformation("Generating comprehensive performance report for period {StartDate} to {EndDate}",
				startDate, endDate);

			var report = new ComprehensivePerformanceReport
			{
				StartDate = startDate,
				EndDate = endDate,
				BaseCurrency = baseCurrency,
				GeneratedAt = DateTime.UtcNow
			};

			try
			{
				// Calculate portfolio-wide performance
				report.PortfolioPerformance = await CalculatePortfolioPerformanceAsync(
					holdings, startDate, endDate, baseCurrency, forceRecalculation);

				// Calculate per-account performance
				report.AccountPerformances = await CalculateAllAccountsPerformanceAsync(
					holdings, startDate, endDate, baseCurrency, forceRecalculation);

				// Calculate per-asset performance
				report.AssetPerformances = await CalculateAllAssetsPerformanceAsync(
					holdings, startDate, endDate, baseCurrency, forceRecalculation);

				// Generate summary statistics
				report.Summary = GeneratePerformanceSummary(report);

				logger.LogInformation("Comprehensive report generated: Portfolio TWR {PortfolioTWR:F2}%, {AccountCount} accounts, {AssetCount} assets",
					report.PortfolioPerformance.TimeWeightedReturn, 
					report.AccountPerformances.Count,
					report.AssetPerformances.Count);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error generating comprehensive performance report");
				throw;
			}

			return report;
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// Core method for calculating performance with different scopes
		/// </summary>
		private async Task<PortfolioPerformance> CalculatePerformanceWithScopeAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			PerformanceScope scope,
			string? scopeIdentifier,
			bool forceRecalculation)
		{
			const string CalculationType = "MarketData";

			// Filter holdings based on scope
			var relevantHoldings = FilterHoldingsByScope(holdings, scope, scopeIdentifier);

			if (!relevantHoldings.Any())
			{
				logger.LogWarning("No holdings found for scope {Scope}:{ScopeId}", scope, scopeIdentifier ?? "All");
				return CreateEmptyPerformance(startDate, endDate, baseCurrency);
			}

			// Check if we have a stored calculation and if recalculation is needed
			if (!forceRecalculation)
			{
				var needsRecalc = await storageService.NeedsRecalculationAsync(
					relevantHoldings, startDate, endDate, baseCurrency, CalculationType, scope, scopeIdentifier);

				if (!needsRecalc)
				{
					var stored = await storageService.GetLatestPerformanceAsync(
						startDate, endDate, baseCurrency, CalculationType, scope, scopeIdentifier);

					if (stored != null)
					{
						logger.LogInformation("Using stored performance for scope {Scope}:{ScopeId}, period {StartDate} to {EndDate}",
							scope, scopeIdentifier ?? "All", startDate, endDate);
						return stored;
					}
				}
			}

			// Calculate fresh performance
			logger.LogInformation("Calculating fresh performance for scope {Scope}:{ScopeId}, period {StartDate} to {EndDate}",
				scope, scopeIdentifier ?? "All", startDate, endDate);

			var performance = await CalculateWithFallback(relevantHoldings, startDate, endDate, baseCurrency);

			// Store the result
			await storageService.StorePerformanceAsync(
				relevantHoldings, startDate, endDate, baseCurrency, CalculationType, performance, scope, scopeIdentifier);

			return performance;
		}

		/// <summary>
		/// Filter holdings based on performance scope
		/// </summary>
		private List<Holding> FilterHoldingsByScope(List<Holding> holdings, PerformanceScope scope, string? scopeIdentifier)
		{
			return scope switch
			{
				PerformanceScope.Account => holdings.Where(h =>
					h.Activities.Any(a => a.Account?.Name == scopeIdentifier)).ToList(),
				PerformanceScope.Asset => holdings.Where(h =>
					h.SymbolProfiles.Any(sp => sp.Symbol == scopeIdentifier)).ToList(),
				_ => holdings
			};
		}

		/// <summary>
		/// Calculate portfolio performance for multiple periods with storage
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> CalculateMultiplePeriodPerformanceAsync(
			List<Holding> holdings,
			List<(string Name, DateTime Start, DateTime End)> periods,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			logger.LogInformation("Calculating performance for {PeriodCount} periods", periods.Count);

			var results = new Dictionary<string, PortfolioPerformance>();

			// Process periods sequentially to avoid database contention
			foreach (var period in periods)
			{
				try
				{
					var performance = await CalculatePortfolioPerformanceAsync(
						holdings, period.Start, period.End, baseCurrency, forceRecalculation);

					results[period.Name] = performance;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to calculate performance for period {PeriodName}", period.Name);
				}
			}

			logger.LogInformation("Completed performance calculation for {CompletedCount}/{TotalCount} periods",
				results.Count, periods.Count);

			return results;
		}

		/// <summary>
		/// Calculate performance with fallback strategy
		/// </summary>
		private async Task<PortfolioPerformance> CalculateWithFallback(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			// Collect activities for the period
			var allActivities = holdings
				.SelectMany(h => h.Activities)
				.Where(a => a.Date >= startDate && a.Date <= endDate)
				.ToList();

			if (!allActivities.Any())
			{
				logger.LogWarning("No activities found for the specified period {StartDate} to {EndDate}",
					startDate, endDate);
				
				return CreateEmptyPerformance(startDate, endDate, baseCurrency);
			}

			// Try market data-driven calculation first (most accurate)
			try
			{
				logger.LogDebug("Attempting market data-driven calculation");
				return await marketDataCalculator.CalculateAccuratePerformanceAsync(
					allActivities, holdings, startDate, endDate, baseCurrency);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Market data-driven calculation failed, falling back to enhanced calculation");

				// Fallback to enhanced calculator
				try
				{
					logger.LogDebug("Attempting enhanced calculation");
					return await enhancedCalculator.CalculatePerformanceAsync(
						allActivities, holdings, startDate, endDate, baseCurrency);
				}
				catch (Exception ex2)
				{
					logger.LogWarning(ex2, "Enhanced calculation failed, using basic calculation");
					
					// Final fallback to basic calculator
					return basicCalculator.CalculateBasicPerformance(
						allActivities, holdings, startDate, endDate, baseCurrency);
				}
			}
		}

		private PortfolioPerformance CreateEmptyPerformance(DateTime startDate, DateTime endDate, Currency baseCurrency)
		{
			return new PortfolioPerformance(
				0, new Money(baseCurrency, 0), 0, 0,
				startDate, endDate, baseCurrency,
				new Money(baseCurrency, 0), new Money(baseCurrency, 0), new Money(baseCurrency, 0));
		}

		/// <summary>
		/// Generate performance summary statistics
		/// </summary>
		private PerformanceSummary GeneratePerformanceSummary(ComprehensivePerformanceReport report)
		{
			var accountTWRs = report.AccountPerformances.Values.Select(p => p.TimeWeightedReturn).ToList();
			var assetTWRs = report.AssetPerformances.Values.Select(p => p.TimeWeightedReturn).ToList();

			return new PerformanceSummary
			{
				BestPerformingAccount = report.AccountPerformances
					.OrderByDescending(kvp => kvp.Value.TimeWeightedReturn)
					.FirstOrDefault(),
				WorstPerformingAccount = report.AccountPerformances
					.OrderBy(kvp => kvp.Value.TimeWeightedReturn)
					.FirstOrDefault(),
				BestPerformingAsset = report.AssetPerformances
					.OrderByDescending(kvp => kvp.Value.TimeWeightedReturn)
					.FirstOrDefault(),
				WorstPerformingAsset = report.AssetPerformances
					.OrderBy(kvp => kvp.Value.TimeWeightedReturn)
					.FirstOrDefault(),
				AverageAccountTWR = accountTWRs.Any() ? accountTWRs.Average() : 0,
				AverageAssetTWR = assetTWRs.Any() ? assetTWRs.Average() : 0,
				AccountTWRStandardDeviation = CalculateStandardDeviation(accountTWRs),
				AssetTWRStandardDeviation = CalculateStandardDeviation(assetTWRs)
			};
		}

		private decimal CalculateStandardDeviation(List<decimal> values)
		{
			if (!values.Any()) return 0;
			
			var average = values.Average();
			var sumOfSquaresOfDifferences = values.Sum(val => (double)Math.Pow((double)(val - average), 2));
			var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
			
			return (decimal)standardDeviation;
		}

		#endregion

		#region Existing Methods (updated for compatibility)

		/// <summary>
		/// Get historical performance data for a specific period
		/// </summary>
		public async Task<List<PortfolioPerformanceSnapshot>> GetPerformanceHistoryAsync(
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			string? calculationType = null)
		{
			return await storageService.GetPerformanceHistoryAsync(startDate, endDate, baseCurrency, calculationType);
		}

		/// <summary>
		/// Get all available performance periods
		/// </summary>
		public async Task<List<(DateTime StartDate, DateTime EndDate, Currency BaseCurrency, string CalculationType, PerformanceScope Scope, string? ScopeIdentifier)>> GetAvailablePeriodsAsync()
		{
			return await storageService.GetAvailablePeriodsAsync();
		}

		/// <summary>
		/// Force refresh of stored performance data
		/// </summary>
		public async Task RefreshPerformanceDataAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			logger.LogInformation("Force refreshing performance data for period {StartDate} to {EndDate}",
				startDate, endDate);

			await CalculatePortfolioPerformanceAsync(holdings, startDate, endDate, baseCurrency, forceRecalculation: true);

			logger.LogInformation("Performance data refreshed successfully");
		}

		/// <summary>
		/// Get storage statistics for monitoring
		/// </summary>
		public async Task<PerformanceStorageStatistics> GetStorageStatisticsAsync()
		{
			return await storageService.GetStorageStatisticsAsync();
		}

		/// <summary>
		/// Clean up old performance snapshots
		/// </summary>
		public async Task CleanupOldSnapshotsAsync(int versionsToKeep = 5)
		{
			await storageService.CleanupOldVersionsAsync(versionsToKeep);
		}

		/// <summary>
		/// Generate a comprehensive performance summary with storage
		/// </summary>
		public async Task<string> GeneratePerformanceSummaryAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			var performance = await CalculatePortfolioPerformanceAsync(
				holdings, startDate, endDate, baseCurrency, forceRecalculation);

			var absoluteReturn = performance.FinalValue.Amount - performance.InitialValue.Amount - performance.NetCashFlows.Amount;
			var totalReturnPercentage = performance.InitialValue.Amount != 0 
				? (absoluteReturn / performance.InitialValue.Amount) * 100 
				: 0;

			// Calculate annualized return
			var days = (performance.EndDate - performance.StartDate).TotalDays;
			var years = days / 365.25;
			var annualizedReturn = years > 0 
				? (decimal)(Math.Pow((double)(1 + performance.TimeWeightedReturn / 100), 1 / years) - 1) * 100
				: performance.TimeWeightedReturn;

			return $"Portfolio Performance Summary ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}):\n" +
				   $"• Time-Weighted Return: {performance.TimeWeightedReturn:F2}%\n" +
				   $"• Annualized Return: {annualizedReturn:F2}%\n" +
				   $"• Total Return: {totalReturnPercentage:F2}%\n" +
				   $"• Absolute Return: {absoluteReturn:F2} {baseCurrency.Symbol}\n" +
				   $"• Total Dividends: {performance.TotalDividends.Amount:F2} {performance.TotalDividends.Currency.Symbol}\n" +
				   $"• Dividend Yield: {performance.DividendYield:F2}%\n" +
				   $"• Currency Impact: {performance.CurrencyImpact:F2}%\n" +
				   $"• Portfolio Value Change: {performance.InitialValue.Amount:F2} ? {performance.FinalValue.Amount:F2} {baseCurrency.Symbol}\n" +
				   $"• Net Cash Flows: {performance.NetCashFlows.Amount:F2} {baseCurrency.Symbol}";
		}

		#endregion

		#region Time Periods Report

		/// <summary>
		/// Get a detailed report of all calculated time periods with summary information
		/// </summary>
		public string GenerateTimePeriodsReport(List<Holding> holdings)
		{
			var allPeriods = CalculateAllMeaningfulTimePeriods(holdings);
			var report = new System.Text.StringBuilder();

			report.AppendLine("=== Dynamic Time Periods Analysis Report ===");
			report.AppendLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			report.AppendLine("");

			if (!allPeriods.Any())
			{
				report.AppendLine("No meaningful time periods found.");
				return report.ToString();
			}

			// Get all activity dates for context
			var allActivities = holdings.SelectMany(h => h.Activities).ToList();
			var firstActivity = allActivities.Min(a => a.Date);
			var lastActivity = allActivities.Max(a => a.Date);

			report.AppendLine($"Portfolio Activity Span: {firstActivity:yyyy-MM-dd} to {lastActivity:yyyy-MM-dd}");
			report.AppendLine($"Total Activity Period: {(lastActivity - firstActivity).TotalDays:F0} days");
			report.AppendLine($"Total Activities: {allActivities.Count}");
			report.AppendLine("");

			// Group and display by category
			var groupedPeriods = allPeriods
				.GroupBy(p => GetPeriodCategoryForReport(p.Name))
				.OrderBy(g => GetCategoryOrderForReport(g.Key));

			foreach (var group in groupedPeriods)
			{
				report.AppendLine($"=== {group.Key} ===");
				
				var sortedPeriods = group.OrderByDescending(p => p.End).ToList();
				
				foreach (var period in sortedPeriods)
				{
					var days = (period.End - period.Start).TotalDays;
					var activitiesInPeriod = allActivities.Count(a => a.Date >= period.Start && a.Date <= period.End);
					
					report.AppendLine($"  {period.Name}:");
					report.AppendLine($"    Period: {period.Start:yyyy-MM-dd} to {period.End:yyyy-MM-dd}");
					report.AppendLine($"    Duration: {days:F0} days");
					report.AppendLine($"    Activities: {activitiesInPeriod}");
				}
				report.AppendLine("");
			}

			// Summary statistics
			report.AppendLine("=== Summary Statistics ===");
			report.AppendLine($"Total Periods Calculated: {allPeriods.Count}");
			
			var periodsByCategory = groupedPeriods.ToDictionary(g => g.Key, g => g.Count());
			foreach (var (category, count) in periodsByCategory.OrderByDescending(kvp => kvp.Value))
			{
				report.AppendLine($"  {category}: {count} periods");
			}

			var avgDuration = allPeriods.Average(p => (p.End - p.Start).TotalDays);
			var longestPeriod = allPeriods.OrderByDescending(p => (p.End - p.Start).TotalDays).First();
			var shortestPeriod = allPeriods.OrderBy(p => (p.End - p.Start).TotalDays).First();

			report.AppendLine("");
			report.AppendLine($"Average Period Duration: {avgDuration:F0} days");
			report.AppendLine($"Longest Period: {longestPeriod.Name} ({(longestPeriod.End - longestPeriod.Start).TotalDays:F0} days)");
			report.AppendLine($"Shortest Period: {shortestPeriod.Name} ({(shortestPeriod.End - shortestPeriod.Start).TotalDays:F0} days)");

			return report.ToString();
		}

		/// <summary>
		/// Categorize periods for reporting
		/// </summary>
		private string GetPeriodCategoryForReport(string periodName)
		{
			if (periodName.Contains("Week")) return "Weekly Periods";
			if (periodName.Contains("Month") && !periodName.Contains("Year") && !periodName.Contains("First")) return "Monthly Periods";
			if (periodName.Contains("Quarter") || periodName.StartsWith("Q")) return "Quarterly Periods";
			if (periodName.Contains("Year") && !periodName.Contains("Rolling")) return "Annual Periods";
			if (periodName.Contains("Rolling")) return "Rolling Periods";
			if (periodName.Contains("Inception")) return "Inception Periods";
			if (periodName.Contains("First")) return "Milestone Periods";
			return "Other Periods";
		}

		/// <summary>
		/// Get category display order for reporting
		/// </summary>
		private int GetCategoryOrderForReport(string category)
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

		#endregion
	}

	/// <summary>
	/// Comprehensive performance report including portfolio, account, and asset breakdowns
	/// </summary>
	public class ComprehensivePerformanceReport
	{
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public Currency BaseCurrency { get; set; } = Currency.EUR;
		public DateTime GeneratedAt { get; set; }

		public PortfolioPerformance PortfolioPerformance { get; set; } = new();
		public Dictionary<string, PortfolioPerformance> AccountPerformances { get; set; } = new();
		public Dictionary<string, PortfolioPerformance> AssetPerformances { get; set; } = new();
		public PerformanceSummary Summary { get; set; } = new();
	}

	/// <summary>
	/// Summary statistics for performance analysis
	/// </summary>
	public class PerformanceSummary
	{
		public KeyValuePair<string, PortfolioPerformance> BestPerformingAccount { get; set; }
		public KeyValuePair<string, PortfolioPerformance> WorstPerformingAccount { get; set; }
		public KeyValuePair<string, PortfolioPerformance> BestPerformingAsset { get; set; }
		public KeyValuePair<string, PortfolioPerformance> WorstPerformingAsset { get; set; }
		public decimal AverageAccountTWR { get; set; }
		public decimal AverageAssetTWR { get; set; }
		public decimal AccountTWRStandardDeviation { get; set; }
		public decimal AssetTWRStandardDeviation { get; set; }
	}
}