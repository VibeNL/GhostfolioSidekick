using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.Services.Implementation;

public class PortfolioValueService : IPortfolioValueService
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrencyExchange _currencyExchange;
	private readonly ILogger<PortfolioValueService> _logger;

	public PortfolioValueService(DatabaseContext dbContext, ICurrencyExchange currencyExchange, ILogger<PortfolioValueService> logger)
	{
		_dbContext = dbContext;
		_currencyExchange = currencyExchange;
		_logger = logger;
	}

	public async Task<List<string>> GetAvailableCurrenciesAsync()
	{
		try
		{
			var activityCurrencies = await _dbContext.Activities
				.Where(a => a is CashDepositWithdrawalActivity)
				.Cast<CashDepositWithdrawalActivity>()
				.Select(a => a.Amount.Currency.Symbol)
				.Distinct()
				.ToListAsync();

			var balanceCurrencies = await _dbContext.Accounts
				.Include(a => a.Balance)
				.SelectMany(a => a.Balance)
				.Select(b => b.Money.Currency.Symbol)
				.Distinct()
				.ToListAsync();

			var currencies = activityCurrencies.Union(balanceCurrencies)
				.Where(c => !string.IsNullOrEmpty(c))
				.OrderBy(c => c)
				.ToList();

			return currencies.Any() ? currencies : new List<string> { "USD", "EUR" };
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading available currencies");
			return new List<string> { "USD", "EUR" };
		}
	}

	public async Task<List<PortfolioValuePoint>> GetPortfolioValueOverTimeAsync(string timeframe, string currency)
	{
		try
		{
			var endDate = DateTime.Today;
			var startDate = GetStartDateFromTimeframe(timeframe);

			var cashFlows = await LoadCashFlows(startDate, endDate, currency);
			var accountBalances = await LoadAccountBalances(startDate, endDate, currency);
			var holdingsValues = await LoadHoldingsValues(startDate, endDate, currency);

			return CalculatePortfolioValueOverTime(cashFlows, accountBalances, holdingsValues, startDate, endDate);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error calculating portfolio value over time for timeframe {Timeframe} and currency {Currency}", timeframe, currency);
			return new List<PortfolioValuePoint>();
		}
	}

	public async Task<PortfolioSummary> GetPortfolioSummaryAsync(List<PortfolioValuePoint> portfolioData, string currency)
	{
		try
		{
			if (!portfolioData.Any())
			{
				return new PortfolioSummary();
			}

			var latestPoint = portfolioData.OrderByDescending(p => p.Date).First();

			var totalReturnAmount = latestPoint.TotalValue - latestPoint.CumulativeInvested;
			var totalReturnPercent = latestPoint.CumulativeInvested != 0
				? (totalReturnAmount / latestPoint.CumulativeInvested) * 100
				: 0;

			return new PortfolioSummary
			{
				CurrentPortfolioValue = $"{latestPoint.TotalValue:N2} {currency}",
				CurrentValueDate = latestPoint.Date.ToString("MMM dd, yyyy"),
				TotalInvestedAmount = $"{latestPoint.CumulativeInvested:N2} {currency}",
				TotalReturnAmount = totalReturnAmount,
				TotalReturnPercent = totalReturnPercent
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error calculating portfolio summary");
			return new PortfolioSummary();
		}
	}

	public async Task<List<AccountBreakdown>> GetPortfolioBreakdownAsync(string currency)
	{
		try
		{
			var accounts = await _dbContext.Accounts
				.Include(a => a.Balance)
				.ToListAsync();

			var breakdown = new List<AccountBreakdown>();
			var targetCurrency = GhostfolioSidekick.Model.Currency.GetCurrency(currency);

			foreach (var account in accounts)
			{
				var latestBalance = account.Balance
					.Where(b => b.Money.Currency.Symbol == currency)
					.OrderByDescending(b => b.Date)
					.FirstOrDefault();

				// Calculate holdings value for this account
				var holdingsValue = await CalculateCurrentHoldingsValueForAccount(account.Id, targetCurrency);

				// Convert cash balance to target currency if needed
				var cashBalance = 0m;
				if (latestBalance != null)
				{
					if (latestBalance.Money.Currency.Symbol == currency)
					{
						cashBalance = latestBalance.Money.Amount;
					}
					else
					{
						var convertedMoney = await _currencyExchange.ConvertMoney(
							latestBalance.Money,
							targetCurrency,
							DateOnly.FromDateTime(DateTime.Today));
						cashBalance = convertedMoney.Amount;
					}
				}

				if (cashBalance > 0 || holdingsValue > 0)
				{
					breakdown.Add(new AccountBreakdown
					{
						AccountName = account.Name,
						CashBalance = cashBalance,
						HoldingsValue = holdingsValue,
						CurrentValue = cashBalance + holdingsValue
					});
				}
			}

			var totalValue = breakdown.Sum(b => b.CurrentValue);
			foreach (var item in breakdown)
			{
				item.PercentageOfPortfolio = totalValue > 0 ? (item.CurrentValue / totalValue) * 100 : 0;
			}

			return breakdown;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading portfolio breakdown for currency {Currency}", currency);
			return new List<AccountBreakdown>();
		}
	}

	private DateTime GetStartDateFromTimeframe(string timeframe)
	{
		var today = DateTime.Today;
		return timeframe switch
		{
			"1m" => today.AddMonths(-1),
			"3m" => today.AddMonths(-3),
			"6m" => today.AddMonths(-6),
			"1y" => today.AddYears(-1),
			"2y" => today.AddYears(-2),
			"all" => DateTime.MinValue,
			_ => today.AddYears(-1)
		};
	}

	private async Task<List<CashFlowPoint>> LoadCashFlows(DateTime startDate, DateTime endDate, string currency)
	{
		var cashDeposits = await _dbContext.Activities
			.OfType<CashDepositWithdrawalActivity>()
			.Where(a => a.Date >= startDate && a.Date <= endDate)
			.OrderBy(a => a.Date)
			.Select(a => new CashFlowPoint
			{
				Date = a.Date,
				Amount = a.Amount.Amount,
				Currency = a.Amount.Currency.Symbol,
				IsDeposit = a.Amount.Amount > 0
			})
			.ToListAsync();

		return cashDeposits.Where(cf => cf.Currency == currency).ToList();
	}

	private async Task<List<BalancePoint>> LoadAccountBalances(DateTime startDate, DateTime endDate, string currency)
	{
		return await _dbContext.Accounts
			.Include(a => a.Balance)
			.SelectMany(a => a.Balance)
			.Where(b => b.Date >= DateOnly.FromDateTime(startDate) &&
					   b.Date <= DateOnly.FromDateTime(endDate) &&
					   b.Money.Currency.Symbol == currency)
			.OrderBy(b => b.Date)
			.Select(b => new BalancePoint
			{
				Date = b.Date.ToDateTime(TimeOnly.MinValue),
				Amount = b.Money.Amount,
				AccountId = b.AccountId
			})
			.ToListAsync();
	}

	private async Task<List<HoldingValuePoint>> LoadHoldingsValues(DateTime startDate, DateTime endDate, string currency)
	{
		try
		{
			// Get all holdings that have activities in the date range or before
			var holdingsWithActivities = await _dbContext.Holdings
				.Include(h => h.Activities)
				.Include(h => h.SymbolProfiles)
				.ThenInclude(sp => sp.MarketData)
				.Where(h => h.Activities.Any(a => a.Date <= endDate))
				.ToListAsync();

			var holdingValuePoints = new List<HoldingValuePoint>();
			var targetCurrency = GhostfolioSidekick.Model.Currency.GetCurrency(currency);

			foreach (var holding in holdingsWithActivities)
			{
				// Calculate position over time for this holding
				var positions = await CalculateHoldingPositionOverTime(holding, startDate, endDate, targetCurrency);
				holdingValuePoints.AddRange(positions);
			}

			return holdingValuePoints;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading holdings values for currency {Currency}", currency);
			return new List<HoldingValuePoint>();
		}
	}

	private async Task<List<HoldingValuePoint>> CalculateHoldingPositionOverTime(GhostfolioSidekick.Model.Holding holding, DateTime startDate, DateTime endDate, GhostfolioSidekick.Model.Currency targetCurrency)
	{
		try
		{
			// Get all buy/sell activities for this holding
			var activities = await _dbContext.Activities
				.OfType<BuySellActivity>()
				.Where(a => a.Holding != null && a.Holding.Id == holding.Id && a.Date <= endDate)
				.OrderBy(a => a.Date)
				.ToListAsync();

			if (!activities.Any())
				return new List<HoldingValuePoint>();

			var positions = new List<HoldingValuePoint>();
			var currentQuantity = 0m;

			// Calculate quantity at start date using AdjustedQuantity (which handles buy/sell signs correctly)
			foreach (var activity in activities.Where(a => a.Date <= startDate))
			{
				// Use AdjustedQuantity which already has correct signs: positive for buy, negative for sell
				currentQuantity += activity.AdjustedQuantity != 0 ? activity.AdjustedQuantity : activity.Quantity;
			}

			// Generate positions for each week in the date range
			var currentDate = startDate;
			while (currentDate <= endDate)
			{
				// Update quantity based on activities up to this date
				foreach (var activity in activities.Where(a => a.Date <= currentDate && a.Date > currentDate.AddDays(-7)))
				{
					// Use AdjustedQuantity which already has correct signs: positive for buy, negative for sell
					currentQuantity += activity.AdjustedQuantity != 0 ? activity.AdjustedQuantity : activity.Quantity;
				}

				if (currentQuantity > 0)
				{
					// Get market price for this date
					var marketPrice = await GetMarketPriceForDate(holding, currentDate, targetCurrency);

					positions.Add(new HoldingValuePoint
					{
						Date = currentDate,
						HoldingId = (int)holding.Id,
						Quantity = currentQuantity,
						Price = marketPrice,
						Value = currentQuantity * marketPrice
					});
				}

				currentDate = currentDate.AddDays(7); // Weekly intervals
			}

			return positions;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error calculating holding position over time for holding {HoldingId}", holding.Id);
			return new List<HoldingValuePoint>();
		}
	}

	private async Task<decimal> GetMarketPriceForDate(GhostfolioSidekick.Model.Holding holding, DateTime date, GhostfolioSidekick.Model.Currency targetCurrency)
	{
		try
		{
			var symbolProfile = holding.SymbolProfiles?.FirstOrDefault();
			if (symbolProfile == null)
				return 0;

			var dateOnly = DateOnly.FromDateTime(date);

			// Try to get market data for the exact date
			var marketData = symbolProfile.MarketData?
				.Where(md => md.Date <= dateOnly)
				.OrderByDescending(md => md.Date)
				.FirstOrDefault();

			if (marketData?.Close != null)
			{
				// Convert the market price to target currency
				var convertedPrice = await _currencyExchange.ConvertMoney(
					marketData.Close,
					targetCurrency,
					dateOnly);
				return convertedPrice.Amount;
			}

			// Fallback: use latest activity price for this holding
			var latestActivity = await _dbContext.Activities
				.OfType<BuySellActivity>()
				.Where(a => a.Holding != null && a.Holding.Id == holding.Id && a.Date <= date)
				.OrderByDescending(a => a.Date)
				.FirstOrDefaultAsync();

			if (latestActivity?.UnitPrice != null)
			{
				var convertedPrice = await _currencyExchange.ConvertMoney(
					latestActivity.UnitPrice,
					targetCurrency,
					dateOnly);
				return convertedPrice.Amount;
			}

			return 0;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting market price for holding {HoldingId} on date {Date}", holding.Id, date);
			return 0;
		}
	}

	private async Task<decimal> CalculateCurrentHoldingsValueForAccount(int accountId, GhostfolioSidekick.Model.Currency targetCurrency)
	{
		try
		{
			// Get all holdings activities for this account
			var activities = await _dbContext.Activities
				.OfType<BuySellActivity>()
				.Where(a => a.Account.Id == accountId && a.Holding != null)
				.Include(a => a.Holding)
				.ThenInclude(h => h!.SymbolProfiles)
				.ThenInclude(sp => sp.MarketData)
				.OrderBy(a => a.Date)
				.ToListAsync();

			var holdingsValue = 0m;
			var holdingQuantities = new Dictionary<long, decimal>();

			// Calculate current quantities for each holding using AdjustedQuantity
			foreach (var activity in activities)
			{
				if (activity.Holding?.Id != null)
				{
					var holdingId = activity.Holding.Id;
					if (!holdingQuantities.ContainsKey(holdingId))
						holdingQuantities[holdingId] = 0;

					// Use AdjustedQuantity which already has correct signs: positive for buy, negative for sell
					holdingQuantities[holdingId] += activity.AdjustedQuantity != 0 ? activity.AdjustedQuantity : activity.Quantity;
				}
			}

			// Calculate value for each holding with positive quantity
			foreach (var kvp in holdingQuantities.Where(h => h.Value > 0))
			{
				var holdingId = kvp.Key;
				var quantity = kvp.Value;

				// Get the holding with its symbol profile and market data
				var holding = await _dbContext.Holdings
					.Include(h => h.SymbolProfiles)
					.ThenInclude(sp => sp.MarketData)
					.FirstOrDefaultAsync(h => h.Id == holdingId);

				if (holding != null)
				{
					var currentPrice = await GetMarketPriceForDate(holding, DateTime.Today, targetCurrency);
					holdingsValue += quantity * currentPrice;
				}
			}

			return holdingsValue;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error calculating current holdings value for account {AccountId}", accountId);
			return 0;
		}
	}

	private List<PortfolioValuePoint> CalculatePortfolioValueOverTime(
		List<CashFlowPoint> cashFlows,
		List<BalancePoint> accountBalances,
		List<HoldingValuePoint> holdingsValues,
		DateTime startDate,
		DateTime endDate)
	{
		var portfolioPoints = new List<PortfolioValuePoint>();
		var allDates = new List<DateTime>();

		// Collect all relevant dates
		allDates.AddRange(cashFlows.Select(cf => cf.Date));
		allDates.AddRange(accountBalances.Select(ab => ab.Date));
		allDates.AddRange(holdingsValues.Select(hv => hv.Date));

		// Add some regular intervals for smoother chart
		var current = startDate;
		while (current <= endDate)
		{
			allDates.Add(current);
			current = current.AddDays(7); // Weekly points
		}

		allDates = allDates.Distinct().OrderBy(d => d).ToList();

		foreach (var date in allDates)
		{
			// Update cumulative cash flow
			var cumulativeCashFlow = cashFlows.Where(cf => cf.Date <= date).Sum(cf => cf.Amount);

			// Get latest account balances up to this date
			var latestBalances = accountBalances
				.Where(ab => ab.Date <= date)
				.GroupBy(ab => ab.AccountId)
				.Select(g => g.OrderByDescending(ab => ab.Date).First())
				.Sum(ab => ab.Amount);

			// Calculate total holdings value at this date
			var totalHoldingsValue = holdingsValues
				.Where(hv => hv.Date <= date)
				.GroupBy(hv => hv.HoldingId)
				.Select(g => g.OrderByDescending(hv => hv.Date).First())
				.Sum(hv => hv.Value);

			portfolioPoints.Add(new PortfolioValuePoint
			{
				Date = date,
				TotalValue = latestBalances + totalHoldingsValue,
				CashValue = latestBalances,
				HoldingsValue = totalHoldingsValue,
				CumulativeInvested = cumulativeCashFlow
			});
		}

		return portfolioPoints.Where(p => p.Date >= startDate).ToList();
	}
}