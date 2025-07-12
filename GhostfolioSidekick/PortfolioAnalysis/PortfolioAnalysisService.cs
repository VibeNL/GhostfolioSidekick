using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Services;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Service to demonstrate portfolio performance analysis capabilities with market data accuracy
	/// </summary>
	public class PortfolioAnalysisService
	{
		private readonly EnhancedPortfolioPerformanceCalculator enhancedCalculator;
		private readonly MarketDataPortfolioPerformanceCalculator marketDataCalculator;
		private readonly PortfolioPerformanceCalculator basicCalculator;
		private readonly ILogger<PortfolioAnalysisService> logger;

		public PortfolioAnalysisService(
			EnhancedPortfolioPerformanceCalculator enhancedCalculator,
			MarketDataPortfolioPerformanceCalculator marketDataCalculator,
			ILogger<PortfolioAnalysisService> logger)
		{
			this.enhancedCalculator = enhancedCalculator;
			this.marketDataCalculator = marketDataCalculator;
			this.basicCalculator = new PortfolioPerformanceCalculator();
			this.logger = logger;
		}

		/// <summary>
		/// Analyze portfolio performance using the most accurate method available
		/// </summary>
		public async Task AnalyzePortfolioPerformanceAsync(
			List<Holding> holdings,
			DateTime? startDate = null,
			DateTime? endDate = null,
			Currency? baseCurrency = null)
		{
			try
			{
				// Set defaults
				startDate ??= DateTime.Now.AddYears(-1);
				endDate ??= DateTime.Now;
				baseCurrency ??= Currency.EUR;

				logger.LogInformation("Starting market data-driven portfolio performance analysis...");

				// Collect all activities from holdings
				var allActivities = holdings
					.SelectMany(h => h.Activities)
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.ToList();

				if (!allActivities.Any())
				{
					logger.LogWarning("No activities found for the specified period");
					return;
				}

				 // Try market data-driven calculation first (most accurate)
				try
				{
					logger.LogInformation("Using market data-driven calculation for maximum accuracy");
					var marketDataPerformance = await marketDataCalculator.CalculateAccuratePerformanceAsync(
						allActivities,
						holdings,
						startDate.Value,
						endDate.Value,
						baseCurrency);

					DisplayPerformanceResults(marketDataPerformance, "Market Data-Driven Analysis");
					
					// Generate detailed valuation report
					await DisplayDetailedValuationReport(holdings, endDate.Value, baseCurrency);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Market data-driven calculation failed, falling back to enhanced calculation");
					
					// Fallback to enhanced calculator
					try
					{
						var enhancedPerformance = await enhancedCalculator.CalculatePerformanceAsync(
							allActivities,
							holdings,
							startDate.Value,
							endDate.Value,
							baseCurrency);

						DisplayPerformanceResults(enhancedPerformance, "Enhanced Analysis (Currency Conversion)");
					}
					catch (Exception ex2)
					{
						logger.LogWarning(ex2, "Enhanced calculation failed, using basic calculation");
						AnalyzePortfolioPerformanceBasic(holdings, startDate, endDate, baseCurrency);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error analyzing portfolio performance");
				throw;
			}
		}

		/// <summary>
		/// Display detailed valuation report using market data
		/// </summary>
		private async Task DisplayDetailedValuationReport(List<Holding> holdings, DateTime date, Currency baseCurrency)
		{
			try
			{
				logger.LogInformation("=== Detailed Portfolio Valuation Report ===");
				var report = await marketDataCalculator.GenerateValuationReportAsync(holdings, date, baseCurrency);
				
				// Split report into lines and log each line separately for better formatting
				var lines = report.Split('\n', StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					logger.LogInformation(line);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to generate detailed valuation report");
			}
		}

		/// <summary>
		/// Analyze portfolio performance using basic calculation (no currency conversion)
		/// </summary>
		public void AnalyzePortfolioPerformanceBasic(
			List<Holding> holdings,
			DateTime? startDate = null,
			DateTime? endDate = null,
			Currency? baseCurrency = null)
		{
			try
			{
				// Set defaults
				startDate ??= DateTime.Now.AddYears(-1);
				endDate ??= DateTime.Now;
				baseCurrency ??= Currency.EUR;

				logger.LogInformation("Starting basic portfolio performance analysis...");

				// Collect all activities from holdings
				var allActivities = holdings
					.SelectMany(h => h.Activities)
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.ToList();

				if (!allActivities.Any())
				{
					logger.LogWarning("No activities found for the specified period");
					return;
				}

				// Calculate performance using basic calculator
				var performance = basicCalculator.CalculateBasicPerformance(
					allActivities,
					holdings,
					startDate.Value,
					endDate.Value,
					baseCurrency);

				// Display results
				DisplayPerformanceResults(performance, "Basic Analysis");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error analyzing portfolio performance");
				throw;
			}
		}

		/// <summary>
		/// Display portfolio performance results in a readable format
		/// </summary>
		private void DisplayPerformanceResults(Model.Portfolio.PortfolioPerformance performance, string analysisType)
		{
			logger.LogInformation("=== Portfolio Performance Analysis ({AnalysisType}) ===", analysisType);
			logger.LogInformation("Period: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}", 
				performance.StartDate, performance.EndDate);
			logger.LogInformation("Base Currency: {Currency}", performance.BaseCurrency.Symbol);
			logger.LogInformation("");

			logger.LogInformation("=== Return Analysis ===");
			logger.LogInformation("Time-Weighted Return: {TWR:F2}%", performance.TimeWeightedReturn);
			logger.LogInformation("Initial Portfolio Value: {InitialValue}", performance.InitialValue);
			logger.LogInformation("Final Portfolio Value: {FinalValue}", performance.FinalValue);
			logger.LogInformation("Net Cash Flows: {NetCashFlows}", performance.NetCashFlows);
			logger.LogInformation("");

			logger.LogInformation("=== Dividend Analysis ===");
			logger.LogInformation("Total Dividends: {TotalDividends}", performance.TotalDividends);
			logger.LogInformation("Dividend Yield: {DividendYield:F2}%", performance.DividendYield);
			logger.LogInformation("");

			logger.LogInformation("=== Currency Impact ===");
			logger.LogInformation("Currency Impact: {CurrencyImpact:F2}%", performance.CurrencyImpact);
			logger.LogInformation("");

			// Calculate additional metrics
			var totalReturn = performance.FinalValue.Amount - performance.InitialValue.Amount - performance.NetCashFlows.Amount;
			var totalReturnPercentage = performance.InitialValue.Amount != 0 
				? (totalReturn / performance.InitialValue.Amount) * 100 
				: 0;

			logger.LogInformation("=== Additional Metrics ===");
			logger.LogInformation("Absolute Return: {AbsoluteReturn:F2} {Currency}", 
				totalReturn, performance.BaseCurrency.Symbol);
			logger.LogInformation("Total Return %: {TotalReturnPercentage:F2}%", totalReturnPercentage);

			// Annualized return calculation
			var days = (performance.EndDate - performance.StartDate).TotalDays;
			var years = days / 365.25;
			if (years > 0)
			{
				var annualizedReturn = (Math.Pow((double)(1 + performance.TimeWeightedReturn / 100), 1 / years) - 1) * 100;
				logger.LogInformation("Annualized Return: {AnnualizedReturn:F2}%", annualizedReturn);
			}

			// Performance quality indicator
			logger.LogInformation("Analysis Quality: {AnalysisType}", analysisType);
		}

		/// <summary>
		/// Generate a comprehensive portfolio performance summary (market data-driven)
		/// </summary>
		public async Task<string> GenerateAccuratePerformanceSummaryAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			try
			{
				var allActivities = holdings
					.SelectMany(h => h.Activities)
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.ToList();

				var performance = await marketDataCalculator.CalculateAccuratePerformanceAsync(
					allActivities,
					holdings,
					startDate,
					endDate,
					baseCurrency);

				return $"Portfolio Performance Summary (Market Data-Driven) ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}):\n" +
					   $"• Time-Weighted Return: {performance.TimeWeightedReturn:F2}%\n" +
					   $"• Total Dividends: {performance.TotalDividends}\n" +
					   $"• Dividend Yield: {performance.DividendYield:F2}%\n" +
					   $"• Currency Impact: {performance.CurrencyImpact:F2}%\n" +
					   $"• Portfolio Value Change: {performance.InitialValue} ? {performance.FinalValue}\n" +
					   $"• Analysis Quality: Market Data-Driven (Most Accurate)";
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Market data-driven summary failed, using enhanced summary");
				return await GeneratePerformanceSummaryAsync(holdings, startDate, endDate, baseCurrency);
			}
		}

		/// <summary>
		/// Generate a simple portfolio performance summary (enhanced version)
		/// </summary>
		public async Task<string> GeneratePerformanceSummaryAsync(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			try
			{
				var allActivities = holdings
					.SelectMany(h => h.Activities)
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.ToList();

				var performance = await enhancedCalculator.CalculatePerformanceAsync(
					allActivities,
					holdings,
					startDate,
					endDate,
					baseCurrency);

				return $"Portfolio Performance Summary (Enhanced) ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}):\n" +
					   $"• Time-Weighted Return: {performance.TimeWeightedReturn:F2}%\n" +
					   $"• Total Dividends: {performance.TotalDividends}\n" +
					   $"• Dividend Yield: {performance.DividendYield:F2}%\n" +
					   $"• Currency Impact: {performance.CurrencyImpact:F2}%\n" +
					   $"• Portfolio Value Change: {performance.InitialValue} ? {performance.FinalValue}\n" +
					   $"• Analysis Quality: Enhanced (Currency Conversion)";
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Enhanced summary failed, using basic summary");
				return GeneratePerformanceSummaryBasic(holdings, startDate, endDate, baseCurrency);
			}
		}

		/// <summary>
		/// Generate a simple portfolio performance summary (basic version)
		/// </summary>
		public string GeneratePerformanceSummaryBasic(
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			var allActivities = holdings
				.SelectMany(h => h.Activities)
				.Where(a => a.Date >= startDate && a.Date <= endDate)
				.ToList();

			var performance = basicCalculator.CalculateBasicPerformance(
				allActivities,
				holdings,
				startDate,
				endDate,
				baseCurrency);

			return $"Portfolio Performance Summary (Basic) ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}):\n" +
				   $"• Time-Weighted Return: {performance.TimeWeightedReturn:F2}%\n" +
				   $"• Total Dividends: {performance.TotalDividends}\n" +
				   $"• Dividend Yield: {performance.DividendYield:F2}%\n" +
				   $"• Currency Impact: {performance.CurrencyImpact:F2}%\n" +
				   $"• Portfolio Value Change: {performance.InitialValue} ? {performance.FinalValue}\n" +
				   $"• Analysis Quality: Basic (No Currency Conversion)";
		}

		/// <summary>
		/// Compare performance between different periods using market data
		/// </summary>
		public async Task ComparePerformancePeriodsAsync(
			List<Holding> holdings,
			List<(string Name, DateTime Start, DateTime End)> periods,
			Currency baseCurrency)
		{
			logger.LogInformation("=== Portfolio Performance Comparison (Market Data-Driven) ===");
			logger.LogInformation("Base Currency: {Currency}", baseCurrency.Symbol);
			logger.LogInformation("");

			foreach (var (name, start, end) in periods)
			{
				try
				{
					var allActivities = holdings
						.SelectMany(h => h.Activities)
						.Where(a => a.Date >= start && a.Date <= end)
						.ToList();

					if (!allActivities.Any())
					{
						logger.LogInformation("{PeriodName}: No activities found", name);
						continue;
					}

					var performance = await marketDataCalculator.CalculateAccuratePerformanceAsync(
						allActivities,
						holdings,
						start,
						end,
						baseCurrency);

					logger.LogInformation("{PeriodName} ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}):", 
						name, start, end);
					logger.LogInformation("  TWR: {TWR:F2}%, Dividends: {Dividends}, Currency Impact: {CurrencyImpact:F2}%", 
						performance.TimeWeightedReturn, performance.TotalDividends, performance.CurrencyImpact);
					logger.LogInformation("  Portfolio Value: {InitialValue} ? {FinalValue}", 
						performance.InitialValue, performance.FinalValue);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error calculating performance for period {PeriodName}", name);
				}
			}
		}

		/// <summary>
		/// Generate portfolio insights based on market data analysis
		/// </summary>
		public async Task GeneratePortfolioInsightsAsync(
			List<Holding> holdings,
			Currency baseCurrency)
		{
			logger.LogInformation("=== Portfolio Insights (Market Data Analysis) ===");

			try
			{
				// Current portfolio value
				var currentValue = await marketDataCalculator.CalculateAccuratePortfolioValueAsync(
					holdings, DateTime.Now, baseCurrency);
				logger.LogInformation("Current Portfolio Value: {CurrentValue}", currentValue);

				// Holdings breakdown
				logger.LogInformation("\n=== Holdings Breakdown ===");
				foreach (var holding in holdings)
				{
					var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
					if (symbolProfile == null) continue;

					var quantity = marketDataCalculator.CalculateQuantityAtDate(holding.Activities, DateTime.Now);
					if (quantity <= 0) continue;

					var holdingValue = await marketDataCalculator.CalculateHoldingValueAsync(holding, DateTime.Now, baseCurrency);
					var percentage = currentValue.Amount != 0 ? (holdingValue.Amount / currentValue.Amount) * 100 : 0;

					logger.LogInformation("{Symbol}: {Value} ({Percentage:F1}% of portfolio)", 
						symbolProfile.Symbol, holdingValue, percentage);
				}

				// Market data quality assessment
				logger.LogInformation("\n=== Market Data Quality ===");
				foreach (var holding in holdings)
				{
					var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
					if (symbolProfile == null) continue;

					var latestMarketData = symbolProfile.MarketData?
						.OrderByDescending(md => md.Date)
						.FirstOrDefault();

					if (latestMarketData != null)
					{
						var daysSinceUpdate = (DateTime.Now.Date - latestMarketData.Date.ToDateTime(TimeOnly.MinValue)).Days;
						logger.LogInformation("{Symbol}: Last price update {Days} days ago ({Date})", 
							symbolProfile.Symbol, daysSinceUpdate, latestMarketData.Date);
					}
					else
					{
						logger.LogWarning("{Symbol}: No market data available", symbolProfile.Symbol);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error generating portfolio insights");
			}
		}
	}
}