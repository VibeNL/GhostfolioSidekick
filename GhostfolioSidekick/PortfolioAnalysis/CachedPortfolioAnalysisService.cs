using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Portfolio;
using GhostfolioSidekick.Model.Services;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Enhanced portfolio analysis service with persistent performance storage
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
			const string CalculationType = "MarketData";

			// Check if we have a stored calculation and if recalculation is needed
			if (!forceRecalculation)
			{
				var needsRecalc = await storageService.NeedsRecalculationAsync(
					holdings, startDate, endDate, baseCurrency, CalculationType);

				if (!needsRecalc)
				{
					var stored = await storageService.GetLatestPerformanceAsync(
						startDate, endDate, baseCurrency, CalculationType);

					if (stored != null)
					{
						logger.LogInformation("Using stored portfolio performance for period {StartDate} to {EndDate}",
							startDate, endDate);
						return stored;
					}
				}
			}

			// Calculate fresh performance using the most accurate method available
			logger.LogInformation("Calculating fresh portfolio performance for period {StartDate} to {EndDate}",
				startDate, endDate);

			var performance = await CalculateWithFallback(holdings, startDate, endDate, baseCurrency);

			// Store the result
			await storageService.StorePerformanceAsync(
				holdings, startDate, endDate, baseCurrency, CalculationType, performance);

			return performance;
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
		/// Get portfolio performance with automatic period detection and storage
		/// </summary>
		public async Task<Dictionary<string, PortfolioPerformance>> GetStandardPerformanceReportAsync(
			List<Holding> holdings,
			Currency baseCurrency,
			bool forceRecalculation = false)
		{
			var now = DateTime.Now;
			var periods = new List<(string Name, DateTime Start, DateTime End)>
			{
				("Last Week", now.AddDays(-7), now),
				("Last Month", now.AddMonths(-1), now),
				("Last Quarter", now.AddMonths(-3), now),
				("Last 6 Months", now.AddMonths(-6), now),
				("Last Year", now.AddYears(-1), now),
				("Year to Date", new DateTime(now.Year, 1, 1), now),
				("Last 2 Years", now.AddYears(-2), now),
				("Last 3 Years", now.AddYears(-3), now)
			};

			// Filter periods that have activities
			var validPeriods = periods.Where(period =>
				holdings.SelectMany(h => h.Activities)
					.Any(a => a.Date >= period.Start && a.Date <= period.End)
			).ToList();

			logger.LogInformation("Generating standard performance report for {ValidPeriods}/{TotalPeriods} periods with activities",
				validPeriods.Count, periods.Count);

			return await CalculateMultiplePeriodPerformanceAsync(holdings, validPeriods, baseCurrency, forceRecalculation);
		}

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
		public async Task<List<(DateTime StartDate, DateTime EndDate, Currency BaseCurrency, string CalculationType)>> GetAvailablePeriodsAsync()
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
				
				// Return empty performance
				return new PortfolioPerformance(
					0, new Money(baseCurrency, 0), 0, 0,
					startDate, endDate, baseCurrency,
					new Money(baseCurrency, 0), new Money(baseCurrency, 0), new Money(baseCurrency, 0));
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
	}
}