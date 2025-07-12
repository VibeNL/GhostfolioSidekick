using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Portfolio;
using GhostfolioSidekick.Model.Services;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Market data-driven portfolio performance calculator for accurate valuations
	/// </summary>
	public class MarketDataPortfolioPerformanceCalculator
	{
		private readonly ICurrencyExchange currencyExchange;
		private readonly PortfolioPerformanceCalculator baseCalculator;
		private readonly ILogger<MarketDataPortfolioPerformanceCalculator> logger;

		public MarketDataPortfolioPerformanceCalculator(
			ICurrencyExchange currencyExchange,
			ILogger<MarketDataPortfolioPerformanceCalculator> logger)
		{
			this.currencyExchange = currencyExchange;
			this.baseCalculator = new PortfolioPerformanceCalculator();
			this.logger = logger;
		}

		/// <summary>
		/// Calculate accurate portfolio performance using market data
		/// </summary>
		public async Task<PortfolioPerformance> CalculateAccuratePerformanceAsync(
			List<Activity> activities,
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			logger.LogInformation("Calculating market data-driven portfolio performance from {StartDate} to {EndDate} in {Currency}",
				startDate, endDate, baseCurrency.Symbol);

			try
			{
				// Filter activities to the specified period
				var periodActivities = activities
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.OrderBy(a => a.Date)
					.ToList();

				// Calculate accurate portfolio values using market data
				var initialValue = await CalculateAccuratePortfolioValueAsync(holdings, startDate, baseCurrency);
				var finalValue = await CalculateAccuratePortfolioValueAsync(holdings, endDate, baseCurrency);

				// Calculate TWR using accurate valuations
				var timeWeightedReturn = await CalculateAccurateTWRAsync(periodActivities, holdings, startDate, endDate, baseCurrency);

				// Calculate dividend metrics with currency conversion
				var dividendMetrics = await CalculateDividendMetricsAsync(periodActivities, baseCurrency);

				// Calculate currency impact
				var currencyImpact = await CalculateCurrencyImpactAsync(periodActivities, holdings, baseCurrency);

				// Calculate net cash flows
				var netCashFlows = await CalculateNetCashFlowsAsync(periodActivities, baseCurrency);

				return new PortfolioPerformance(
					timeWeightedReturn,
					dividendMetrics.TotalDividends,
					dividendMetrics.DividendYield,
					currencyImpact,
					startDate,
					endDate,
					baseCurrency,
					initialValue,
					finalValue,
					netCashFlows);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error calculating market data-driven portfolio performance");
				
				// Fallback to basic calculation
				logger.LogWarning("Falling back to basic performance calculation");
				return baseCalculator.CalculateBasicPerformance(activities, holdings, startDate, endDate, baseCurrency);
			}
		}

		/// <summary>
		/// Calculate accurate portfolio value using market data
		/// </summary>
		public async Task<Money> CalculateAccuratePortfolioValueAsync(
			List<Holding> holdings, 
			DateTime date, 
			Currency baseCurrency)
		{
			var totalValue = new Money(baseCurrency, 0);

			foreach (var holding in holdings)
			{
				try
				{
					var holdingValue = await CalculateHoldingValueAsync(holding, date, baseCurrency);
					totalValue = totalValue.Add(holdingValue);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error calculating value for holding {Symbol}", 
						holding.SymbolProfiles.FirstOrDefault()?.Symbol ?? "Unknown");
				}
			}

			logger.LogDebug("Portfolio value on {Date}: {Value}", date, totalValue);
			return totalValue;
		}

		/// <summary>
		/// Calculate holding value using market data
		/// </summary>
		public async Task<Money> CalculateHoldingValueAsync(Holding holding, DateTime date, Currency baseCurrency)
		{
			// Calculate quantity at the given date
			var quantity = CalculateQuantityAtDate(holding.Activities, date);
			
			if (quantity <= 0)
			{
				return new Money(baseCurrency, 0);
			}

			var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
			if (symbolProfile == null)
			{
				logger.LogWarning("No symbol profile found for holding");
				return new Money(baseCurrency, 0);
			}

			// Get market price at the specified date
			var marketPrice = GetMarketPriceAtDate(symbolProfile, date);
			
			if (marketPrice == null)
			{
				logger.LogDebug("No market data found for {Symbol} on {Date}, using fallback estimation", 
					symbolProfile.Symbol, date);
				return await EstimateHoldingValueWithoutMarketData(holding, date, baseCurrency, quantity);
			}

			// Calculate value in holding's currency
			var holdingValue = new Money(marketPrice.Currency, quantity * marketPrice.Amount);

			// Convert to base currency if needed
			return await ConvertToCurrencyAsync(holdingValue, baseCurrency, date);
		}

		/// <summary>
		/// Get market price at a specific date using closest available data
		/// </summary>
		private Money? GetMarketPriceAtDate(SymbolProfile symbolProfile, DateTime date)
		{
			if (symbolProfile.MarketData == null || !symbolProfile.MarketData.Any())
			{
				return null;
			}

			var targetDate = DateOnly.FromDateTime(date);
			
			// Try to find exact date match first
			var exactMatch = symbolProfile.MarketData.FirstOrDefault(md => md.Date == targetDate);
			if (exactMatch != null)
			{
				return exactMatch.Close;
			}

			// Find closest date before the target date
			var closestBefore = symbolProfile.MarketData
				.Where(md => md.Date <= targetDate)
				.OrderByDescending(md => md.Date)
				.FirstOrDefault();

			if (closestBefore != null)
			{
				return closestBefore.Close;
			}

			// If no data before target date, use earliest available data
			var earliest = symbolProfile.MarketData
				.OrderBy(md => md.Date)
				.FirstOrDefault();

			return earliest?.Close;
		}

		/// <summary>
		/// Estimate holding value when market data is not available
		/// </summary>
		private async Task<Money> EstimateHoldingValueWithoutMarketData(
			Holding holding, 
			DateTime date, 
			Currency baseCurrency, 
			decimal quantity)
		{
			// Use last known transaction price as fallback
			var lastTransaction = holding.Activities
				.OfType<BuySellActivity>()
				.Where(a => a.Date <= date && a.UnitPrice != null)
				.OrderByDescending(a => a.Date)
				.FirstOrDefault();

			if (lastTransaction != null)
			{
				var estimatedValue = new Money(lastTransaction.UnitPrice.Currency, quantity * lastTransaction.UnitPrice.Amount);
				return await ConvertToCurrencyAsync(estimatedValue, baseCurrency, date);
			}

			logger.LogWarning("No market data or transaction history found for valuation on {Date}", date);
			return new Money(baseCurrency, 0);
		}

		/// <summary>
		/// Calculate accurate Time-Weighted Return using market data
		/// </summary>
		private async Task<decimal> CalculateAccurateTWRAsync(
			List<Activity> activities,
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			try
			{
				// Create periods based on cash flow dates
				var cashFlowDates = activities
					.OfType<CashDepositWithdrawalActivity>()
					.Select(a => a.Date)
					.Distinct()
					.OrderBy(d => d)
					.ToList();

				// Add start and end dates
				var allDates = new List<DateTime> { startDate };
				allDates.AddRange(cashFlowDates);
				allDates.Add(endDate);
				allDates = allDates.Distinct().OrderBy(d => d).ToList();

				decimal cumulativeReturn = 1.0m;

				for (int i = 0; i < allDates.Count - 1; i++)
				{
					var periodStart = allDates[i];
					var periodEnd = allDates[i + 1];

					// Calculate accurate portfolio values for this period
					var startValue = await CalculateAccuratePortfolioValueAsync(holdings, periodStart, baseCurrency);
					var endValue = await CalculateAccuratePortfolioValueAsync(holdings, periodEnd, baseCurrency);

					// Calculate cash flows during this period
					var periodCashFlow = await CalculatePeriodCashFlowAsync(activities, periodStart, periodEnd, baseCurrency);

					// Calculate period return using TWR formula
					if (startValue.Amount > 0)
					{
						var netGain = endValue.Amount - startValue.Amount - periodCashFlow.Amount;
						var periodReturn = netGain / startValue.Amount;
						cumulativeReturn *= (1 + periodReturn);

						logger.LogDebug("Period {Start} to {End}: Start={StartValue}, End={EndValue}, CashFlow={CashFlow}, Return={Return:P2}",
							periodStart, periodEnd, startValue, endValue, periodCashFlow, periodReturn);
					}
				}

				var totalReturn = (cumulativeReturn - 1) * 100;
				logger.LogInformation("Calculated accurate TWR: {TWR:F2}%", totalReturn);
				return totalReturn;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error calculating accurate TWR");
				throw;
			}
		}

		/// <summary>
		/// Calculate cash flow for a specific period
		/// </summary>
		private async Task<Money> CalculatePeriodCashFlowAsync(
			List<Activity> activities,
			DateTime periodStart,
			DateTime periodEnd,
			Currency baseCurrency)
		{
			var cashFlow = new Money(baseCurrency, 0);
			
			var periodCashFlows = activities
				.OfType<CashDepositWithdrawalActivity>()
				.Where(a => a.Date >= periodStart && a.Date < periodEnd);

			foreach (var activity in periodCashFlows)
			{
				var convertedAmount = await ConvertToCurrencyAsync(activity.Amount, baseCurrency, activity.Date);
				cashFlow = cashFlow.Add(convertedAmount);
			}

			return cashFlow;
		}

		/// <summary>
		/// Calculate dividend metrics with currency conversion
		/// </summary>
		private async Task<(Money TotalDividends, decimal DividendYield)> CalculateDividendMetricsAsync(
			List<Activity> activities,
			Currency baseCurrency)
		{
			var dividendActivities = activities.OfType<DividendActivity>().ToList();
			var totalDividends = new Money(baseCurrency, 0);

			foreach (var dividend in dividendActivities)
			{
				try
				{
					var convertedAmount = await ConvertToCurrencyAsync(dividend.Amount, baseCurrency, dividend.Date);
					totalDividends = totalDividends.Add(convertedAmount);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error converting dividend amount for {Date}", dividend.Date);
					// Add without conversion as fallback if same currency
					if (dividend.Amount.Currency.Symbol == baseCurrency.Symbol)
					{
						totalDividends = totalDividends.Add(dividend.Amount);
					}
				}
			}

			// Calculate dividend yield based on average portfolio value
			var avgPortfolioValue = await CalculateAveragePortfolioValueAsync(activities, baseCurrency);
			var dividendYield = avgPortfolioValue.Amount != 0 
				? (totalDividends.Amount / avgPortfolioValue.Amount) * 100 
				: 0;

			return (totalDividends, dividendYield);
		}

		/// <summary>
		/// Calculate average portfolio value during the period
		/// </summary>
		private async Task<Money> CalculateAveragePortfolioValueAsync(List<Activity> activities, Currency baseCurrency)
		{
			if (!activities.Any())
			{
				return new Money(baseCurrency, 0);
			}

			// For now, use simple average of start and end values
			// In a more sophisticated implementation, you could sample portfolio values at regular intervals
			var totalInvested = new Money(baseCurrency, 0);
			var investmentActivities = activities.OfType<BuySellActivity>().Where(a => a.Quantity > 0);

			foreach (var activity in investmentActivities)
			{
				var investmentValue = new Money(activity.UnitPrice.Currency, activity.Quantity * activity.UnitPrice.Amount);
				var convertedValue = await ConvertToCurrencyAsync(investmentValue, baseCurrency, activity.Date);
				totalInvested = totalInvested.Add(convertedValue);
			}

			return totalInvested;
		}

		/// <summary>
		/// Calculate currency impact with detailed analysis
		/// </summary>
		private async Task<decimal> CalculateCurrencyImpactAsync(
			List<Activity> activities,
			List<Holding> holdings,
			Currency baseCurrency)
		{
			try
			{
				var foreignValue = 0m;
				var totalValue = 0m;

				foreach (var holding in holdings)
				{
					var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
					if (symbolProfile == null) continue;

					var quantity = CalculateQuantityAtDate(holding.Activities, DateTime.Now);
					if (quantity <= 0) continue;

					var currentPrice = GetMarketPriceAtDate(symbolProfile, DateTime.Now);
					if (currentPrice == null) continue;

					var holdingValue = quantity * currentPrice.Amount;
					var convertedValue = await ConvertToCurrencyAsync(
						new Money(currentPrice.Currency, holdingValue), 
						baseCurrency, 
						DateTime.Now);

					totalValue += convertedValue.Amount;

					if (currentPrice.Currency.Symbol != baseCurrency.Symbol)
					{
						foreignValue += convertedValue.Amount;
					}
				}

				return totalValue != 0 ? (foreignValue / totalValue) * 100 : 0;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error calculating currency impact");
				return 0;
			}
		}

		/// <summary>
		/// Calculate net cash flows with currency conversion
		/// </summary>
		private async Task<Money> CalculateNetCashFlowsAsync(List<Activity> activities, Currency baseCurrency)
		{
			var netCashFlow = new Money(baseCurrency, 0);
			var cashFlowActivities = activities.OfType<CashDepositWithdrawalActivity>();

			foreach (var activity in cashFlowActivities)
			{
				try
				{
					var convertedAmount = await ConvertToCurrencyAsync(activity.Amount, baseCurrency, activity.Date);
					netCashFlow = netCashFlow.Add(convertedAmount);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error converting cash flow amount for {Date}", activity.Date);
					// Add without conversion if same currency
					if (activity.Amount.Currency.Symbol == baseCurrency.Symbol)
					{
						netCashFlow = netCashFlow.Add(activity.Amount);
					}
				}
			}

			return netCashFlow;
		}

		/// <summary>
		/// Calculate quantity of holdings at a specific date
		/// </summary>
		public decimal CalculateQuantityAtDate(List<Activity> activities, DateTime date)
		{
			return activities
				.Where(a => a.Date <= date)
				.OfType<BuySellActivity>()
				.Sum(a => a.Quantity);
		}

		/// <summary>
		/// Convert money to target currency using historical exchange rates
		/// </summary>
		private async Task<Money> ConvertToCurrencyAsync(Money amount, Currency targetCurrency, DateTime date)
		{
			if (amount.Currency.Symbol == targetCurrency.Symbol)
			{
				return amount;
			}

			try
			{
				return await currencyExchange.ConvertMoney(amount, targetCurrency, DateOnly.FromDateTime(date));
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Currency conversion failed for {Amount} to {TargetCurrency} on {Date}, using 1:1 rate",
					amount, targetCurrency.Symbol, date);
				return new Money(targetCurrency, amount.Amount); // Fallback to 1:1 conversion
			}
		}

		/// <summary>
		/// Generate detailed portfolio valuation report
		/// </summary>
		public async Task<string> GenerateValuationReportAsync(
			List<Holding> holdings, 
			DateTime date, 
			Currency baseCurrency)
		{
			var report = new System.Text.StringBuilder();
			report.AppendLine($"Portfolio Valuation Report - {date:yyyy-MM-dd}");
			report.AppendLine($"Base Currency: {baseCurrency.Symbol}");
			report.AppendLine();

			var totalValue = new Money(baseCurrency, 0);

			foreach (var holding in holdings)
			{
				var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
				if (symbolProfile == null) continue;

				var quantity = CalculateQuantityAtDate(holding.Activities, date);
				if (quantity <= 0) continue;

				var marketPrice = GetMarketPriceAtDate(symbolProfile, date);
				var holdingValue = await CalculateHoldingValueAsync(holding, date, baseCurrency);

				totalValue = totalValue.Add(holdingValue);

				report.AppendLine($"Symbol: {symbolProfile.Symbol}");
				report.AppendLine($"  Quantity: {quantity:F4}");
				report.AppendLine($"  Market Price: {marketPrice?.Amount:F2} {marketPrice?.Currency.Symbol ?? "N/A"}");
				report.AppendLine($"  Value ({baseCurrency.Symbol}): {holdingValue.Amount:F2}");
				report.AppendLine();
			}

			report.AppendLine($"Total Portfolio Value: {totalValue.Amount:F2} {baseCurrency.Symbol}");
			return report.ToString();
		}
	}
}