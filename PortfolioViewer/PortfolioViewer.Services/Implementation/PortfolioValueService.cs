using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.Services.Implementation;

public class PortfolioValueService : IPortfolioValueService
{
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<PortfolioValueService> _logger;

    public PortfolioValueService(DatabaseContext dbContext, ILogger<PortfolioValueService> logger)
    {
        _dbContext = dbContext;
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
            var holdingsValues = new List<HoldingValuePoint>(); // Simplified for now

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

            foreach (var account in accounts)
            {
                var latestBalance = account.Balance
                    .Where(b => b.Money.Currency.Symbol == currency)
                    .OrderByDescending(b => b.Date)
                    .FirstOrDefault();

                if (latestBalance != null)
                {
                    breakdown.Add(new AccountBreakdown
                    {
                        AccountName = account.Name,
                        CashBalance = latestBalance.Money.Amount,
                        HoldingsValue = 0, // Would need to calculate from holdings in this account
                        CurrentValue = latestBalance.Money.Amount
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

            portfolioPoints.Add(new PortfolioValuePoint
            {
                Date = date,
                TotalValue = latestBalances,
                CashValue = latestBalances,
                HoldingsValue = 0,
                CumulativeInvested = cumulativeCashFlow
            });
        }

        return portfolioPoints.Where(p => p.Date >= startDate).ToList();
    }
}