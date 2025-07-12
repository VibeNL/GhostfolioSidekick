using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Portfolio;

namespace GhostfolioSidekick.Model.Services
{
	/// <summary>
	/// Service for calculating basic portfolio performance metrics
	/// </summary>
	public class PortfolioPerformanceCalculator
	{
		/// <summary>
		/// Calculate comprehensive portfolio performance for a given period (simplified version)
		/// </summary>
		/// <param name="activities">Portfolio activities to analyze</param>
		/// <param name="holdings">Current holdings for valuation</param>
		/// <param name="startDate">Start date for performance calculation</param>
		/// <param name="endDate">End date for performance calculation</param>
		/// <param name="baseCurrency">Base currency for all calculations</param>
		/// <returns>Portfolio performance metrics</returns>
		public PortfolioPerformance CalculateBasicPerformance(
			List<Activity> activities,
			List<Holding> holdings,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency)
		{
			// Filter activities to the specified period
			var periodActivities = activities
				.Where(a => a.Date >= startDate && a.Date <= endDate)
				.OrderBy(a => a.Date)
				.ToList();

			// Calculate basic metrics without currency conversion
			var dividendMetrics = CalculateDividendMetrics(periodActivities, baseCurrency);
			var cashFlows = CalculateNetCashFlows(periodActivities, baseCurrency);
			
			// Simplified TWR calculation (would need market data for accuracy)
			var twr = CalculateSimplifiedTWR(periodActivities, baseCurrency);
			
			// Basic currency impact (percentage of foreign currency activities)
			var currencyImpact = CalculateCurrencyImpact(periodActivities, baseCurrency);

			// Placeholder values for portfolio value (would need market data)
			var initialValue = new Money(baseCurrency, 10000); // Placeholder
			var finalValue = new Money(baseCurrency, 11000);   // Placeholder

			return new PortfolioPerformance(
				twr,
				dividendMetrics.TotalDividends,
				dividendMetrics.DividendYield,
				currencyImpact,
				startDate,
				endDate,
				baseCurrency,
				initialValue,
				finalValue,
				cashFlows);
		}

		/// <summary>
		/// Calculate Time-Weighted Return (simplified version without currency conversion)
		/// </summary>
		private decimal CalculateSimplifiedTWR(List<Activity> activities, Currency baseCurrency)
		{
			// Simple calculation based on buy/sell activities in base currency only
			var baseActivities = activities
				.Where(a => GetActivityCurrency(a)?.Symbol == baseCurrency.Symbol)
				.ToList();

			var buyActivities = baseActivities.OfType<BuySellActivity>().Where(a => a.Quantity > 0).ToList();
			var sellActivities = baseActivities.OfType<BuySellActivity>().Where(a => a.Quantity < 0).ToList();

			if (!buyActivities.Any()) return 0;

			var totalInvested = buyActivities.Sum(a => a.Quantity * a.UnitPrice.Amount);
			var totalRealized = sellActivities.Sum(a => Math.Abs(a.Quantity) * a.UnitPrice.Amount);

			if (totalInvested == 0) return 0;

			// Simplified return calculation
			return ((totalRealized - totalInvested) / totalInvested) * 100;
		}

		/// <summary>
		/// Calculate dividend-related metrics (simplified)
		/// </summary>
		private (Money TotalDividends, decimal DividendYield) CalculateDividendMetrics(
			List<Activity> activities,
			Currency baseCurrency)
		{
			var dividendActivities = activities
				.OfType<DividendActivity>()
				.Where(d => d.Amount.Currency.Symbol == baseCurrency.Symbol)
				.ToList();
			
			var totalDividends = new Money(baseCurrency, 
				dividendActivities.Sum(d => d.Amount.Amount));

			// Simple dividend yield calculation based on total invested
			var totalInvested = activities
				.OfType<BuySellActivity>()
				.Where(a => a.Quantity > 0 && a.UnitPrice.Currency.Symbol == baseCurrency.Symbol)
				.Sum(a => a.Quantity * a.UnitPrice.Amount);

			var dividendYield = totalInvested != 0 
				? (totalDividends.Amount / totalInvested) * 100 
				: 0;

			return (totalDividends, dividendYield);
		}

		/// <summary>
		/// Calculate currency impact on portfolio performance
		/// </summary>
		private decimal CalculateCurrencyImpact(List<Activity> activities, Currency baseCurrency)
		{
			var foreignActivities = activities
				.Where(a => GetActivityCurrency(a)?.Symbol != baseCurrency.Symbol)
				.ToList();

			if (!foreignActivities.Any() || !activities.Any())
			{
				return 0; // No foreign currency exposure
			}

			// Simple calculation: percentage of foreign activities
			return (decimal)foreignActivities.Count / activities.Count * 100;
		}

		/// <summary>
		/// Calculate net cash flows for a period
		/// </summary>
		private Money CalculateNetCashFlows(List<Activity> activities, Currency baseCurrency)
		{
			var netCashFlow = new Money(baseCurrency, 0);

			var cashFlowActivities = activities
				.OfType<CashDepositWithdrawalActivity>()
				.Where(a => a.Amount.Currency.Symbol == baseCurrency.Symbol);

			foreach (var activity in cashFlowActivities)
			{
				netCashFlow = netCashFlow.Add(activity.Amount);
			}

			return netCashFlow;
		}

		/// <summary>
		/// Calculate current quantity of an asset based on activities
		/// </summary>
		private decimal CalculateCurrentQuantity(List<Activity> activities)
		{
			decimal quantity = 0;

			foreach (var activity in activities.OrderBy(a => a.Date))
			{
				if (activity is BuySellActivity buySell)
				{
					quantity += buySell.Quantity;
				}
			}

			return quantity;
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

		/// <summary>
		/// Create periods for TWR calculation based on cash flow dates
		/// </summary>
		public List<PortfolioPeriod> CreatePeriodsForTWR(
			List<Activity> activities,
			DateTime startDate,
			DateTime endDate)
		{
			// Get cash flow dates
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

			var periods = new List<PortfolioPeriod>();

			for (int i = 0; i < allDates.Count - 1; i++)
			{
				var periodStart = allDates[i];
				var periodEnd = allDates[i + 1];

				var periodActivities = activities
					.Where(a => a.Date >= periodStart && a.Date < periodEnd)
					.ToList();

				// Placeholder values (would need market data for real calculation)
				var startValue = new Money(Currency.EUR, 1000);
				var endValue = new Money(Currency.EUR, 1100);
				var cashFlow = new Money(Currency.EUR, 0);

				// Calculate actual cash flow for the period
				foreach (var activity in periodActivities.OfType<CashDepositWithdrawalActivity>())
				{
					cashFlow = cashFlow.Add(activity.Amount);
				}

				periods.Add(new PortfolioPeriod(periodStart, periodEnd, startValue, endValue, cashFlow, periodActivities));
			}

			return periods;
		}
	}
}