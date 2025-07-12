using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Portfolio;
using GhostfolioSidekick.Model.Services;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioAnalysis
{
	/// <summary>
	/// Enhanced portfolio performance calculator with currency conversion capabilities
	/// </summary>
	public class EnhancedPortfolioPerformanceCalculator
	{
		private readonly ICurrencyExchange currencyExchange;
		private readonly PortfolioPerformanceCalculator baseCalculator;
		private readonly ILogger<EnhancedPortfolioPerformanceCalculator> logger;

		public EnhancedPortfolioPerformanceCalculator(
			ICurrencyExchange currencyExchange,
			ILogger<EnhancedPortfolioPerformanceCalculator> logger)
		{
			this.currencyExchange = currencyExchange;
			this.baseCalculator = new PortfolioPerformanceCalculator();
			this.logger = logger;
		}

		/// <summary>
		/// Calculate comprehensive portfolio performance with currency conversion
		/// </summary>
		public async Task<PortfolioPerformance> CalculatePerformanceAsync(
			List<Activity> activities,
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			logger.LogInformation("Calculating enhanced portfolio performance from {StartDate} to {EndDate} in {Currency}",
				startDate, endDate, baseCurrency.Symbol);

			try
			{
				// Filter activities to the specified period
				var periodActivities = activities
					.Where(a => a.Date >= startDate && a.Date <= endDate)
					.OrderBy(a => a.Date)
					.ToList();

				// Calculate enhanced metrics with currency conversion
				var timeWeightedReturn = await CalculateTimeWeightedReturnAsync(periodActivities, holdings, startDate, endDate, baseCurrency);
				var dividendMetrics = await CalculateDividendMetricsAsync(periodActivities, baseCurrency);
				var currencyImpact = await CalculateCurrencyImpactAsync(periodActivities, holdings, baseCurrency);
				var netCashFlows = await CalculateNetCashFlowsAsync(periodActivities, baseCurrency);

				// Estimate portfolio values (would need market data integration for accuracy)
				var initialValue = await EstimatePortfolioValueAsync(holdings, startDate, baseCurrency);
				var finalValue = await EstimatePortfolioValueAsync(holdings, endDate, baseCurrency);

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
				logger.LogError(ex, "Error calculating portfolio performance");
				
				// Fallback to basic calculation
				logger.LogWarning("Falling back to basic performance calculation");
				return baseCalculator.CalculateBasicPerformance(activities, holdings, startDate, endDate, baseCurrency);
			}
		}

		/// <summary>
		/// Calculate Time-Weighted Return with currency conversion
		/// </summary>
		private async Task<decimal> CalculateTimeWeightedReturnAsync(
			List<Activity> activities,
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			try
			{
				// Create periods based on cash flow dates
				var periods = baseCalculator.CreatePeriodsForTWR(activities, startDate, endDate);
				
				decimal cumulativeReturn = 1.0m;
				
				foreach (var period in periods)
				{
					// Convert values to base currency
					var convertedStartValue = await ConvertToCurrencyAsync(period.StartValue, baseCurrency, period.StartDate);
					var convertedEndValue = await ConvertToCurrencyAsync(period.EndValue, baseCurrency, period.EndDate);
					var convertedCashFlow = await ConvertToCurrencyAsync(period.CashFlow, baseCurrency, period.StartDate);

					// Calculate period return
					if (convertedStartValue.Amount > 0)
					{
						var netGain = convertedEndValue.Amount - convertedStartValue.Amount - convertedCashFlow.Amount;
						var periodReturn = netGain / convertedStartValue.Amount;
						cumulativeReturn *= (1 + periodReturn);
					}
				}

				return (cumulativeReturn - 1) * 100; // Convert to percentage
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error calculating TWR with currency conversion, using simplified calculation");
				return baseCalculator.CalculateBasicPerformance(activities, [], startDate, endDate, baseCurrency).TimeWeightedReturn;
			}
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
					// Add without conversion as fallback
					if (dividend.Amount.Currency.Symbol == baseCurrency.Symbol)
					{
						totalDividends = totalDividends.Add(dividend.Amount);
					}
				}
			}

			// Calculate average investment for dividend yield
			var totalInvestedAmount = 0m;
			var investmentActivities = activities.OfType<BuySellActivity>().Where(a => a.Quantity > 0).ToList();

			foreach (var investment in investmentActivities)
			{
				try
				{
					var convertedAmount = await ConvertToCurrencyAsync(
						new Money(investment.UnitPrice.Currency, investment.Quantity * investment.UnitPrice.Amount),
						baseCurrency,
						investment.Date);
					totalInvestedAmount += convertedAmount.Amount;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error converting investment amount for {Date}", investment.Date);
				}
			}

			var dividendYield = totalInvestedAmount != 0 
				? (totalDividends.Amount / totalInvestedAmount) * 100 
				: 0;

			return (totalDividends, dividendYield);
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
				var foreignActivities = activities
					.Where(a => GetActivityCurrency(a)?.Symbol != baseCurrency.Symbol)
					.ToList();

				if (!foreignActivities.Any())
				{
					return 0;
				}

				// Calculate value of foreign currency activities vs total
				var foreignValue = 0m;
				var totalValue = 0m;

				foreach (var activity in activities)
				{
					var activityValue = GetActivityValue(activity);
					if (activityValue != null)
					{
						try
						{
							var convertedValue = await ConvertToCurrencyAsync(activityValue, baseCurrency, activity.Date);
							totalValue += Math.Abs(convertedValue.Amount);

							if (GetActivityCurrency(activity)?.Symbol != baseCurrency.Symbol)
							{
								foreignValue += Math.Abs(convertedValue.Amount);
							}
						}
						catch (Exception ex)
						{
							logger.LogWarning(ex, "Error converting activity value for currency impact calculation");
						}
					}
				}

				return totalValue != 0 ? (foreignValue / totalValue) * 100 : 0;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error calculating detailed currency impact, using simple calculation");
				return baseCalculator.CalculateBasicPerformance(activities, holdings, DateTime.MinValue, DateTime.MaxValue, baseCurrency).CurrencyImpact;
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
		/// Estimate portfolio value at a specific date
		/// </summary>
		private async Task<Money> EstimatePortfolioValueAsync(List<Holding> holdings, DateTime date, Currency baseCurrency)
		{
			// This is a simplified estimation - in practice, you'd need current market data
			var totalValue = new Money(baseCurrency, 0);

			foreach (var holding in holdings)
			{
				try
				{
					var activities = holding.Activities.Where(a => a.Date <= date).ToList();
					var quantity = CalculateQuantityAtDate(activities, date);

					if (quantity > 0)
					{
						// Estimate value based on last known price or average price
						var lastBuyActivity = activities
							.OfType<BuySellActivity>()
							.Where(a => a.Quantity > 0)
							.OrderByDescending(a => a.Date)
							.FirstOrDefault();

						if (lastBuyActivity != null)
						{
							var estimatedValue = new Money(lastBuyActivity.UnitPrice.Currency, quantity * lastBuyActivity.UnitPrice.Amount);
							var convertedValue = await ConvertToCurrencyAsync(estimatedValue, baseCurrency, date);
							totalValue = totalValue.Add(convertedValue);
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error estimating value for holding");
				}
			}

			return totalValue;
		}

		/// <summary>
		/// Convert money to target currency
		/// </summary>
		private async Task<Money> ConvertToCurrencyAsync(Money amount, Currency targetCurrency, DateTime date)
		{
			if (amount.Currency.Symbol == targetCurrency.Symbol)
			{
				return amount;
			}

			return await currencyExchange.ConvertMoney(amount, targetCurrency, DateOnly.FromDateTime(date));
		}

		/// <summary>
		/// Calculate quantity at a specific date
		/// </summary>
		private decimal CalculateQuantityAtDate(List<Activity> activities, DateTime date)
		{
			return activities
				.Where(a => a.Date <= date)
				.OfType<BuySellActivity>()
				.Sum(a => a.Quantity);
		}

		/// <summary>
		/// Get activity value
		/// </summary>
		private Money? GetActivityValue(Activity activity)
		{
			return activity switch
			{
				BuySellActivity buySell => new Money(buySell.UnitPrice.Currency, Math.Abs(buySell.Quantity) * buySell.UnitPrice.Amount),
				CashDepositWithdrawalActivity cash => cash.Amount,
				DividendActivity dividend => dividend.Amount,
				FeeActivity fee => fee.Amount,
				InterestActivity interest => interest.Amount,
				_ => null
			};
		}

		/// <summary>
		/// Get currency from activity
		/// </summary>
		private Currency? GetActivityCurrency(Activity activity)
		{
			return activity switch
			{
				BuySellActivity buySell => buySell.UnitPrice.Currency,
				CashDepositWithdrawalActivity cash => cash.Amount.Currency,
				DividendActivity dividend => dividend.Amount.Currency,
				FeeActivity fee => fee.Amount.Currency,
				InterestActivity interest => interest.Amount.Currency,
				_ => null
			};
		}
	}
}