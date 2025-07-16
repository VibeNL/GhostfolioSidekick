using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Radzen.Blazor;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class PortfolioValueOverTime
    {
        [Inject]
        private DatabaseContext DbContext { get; set; } = default!;

        private bool isLoading = true;
        private string selectedTimeframe = "1y";
        private string selectedCurrency = "USD";

        private List<string>? AvailableCurrencies;
        private List<PortfolioValuePoint>? PortfolioData;
        private List<AccountBreakdown>? PortfolioBreakdown;
        
        private string CurrentPortfolioValue = "N/A";
        private string CurrentValueDate = "N/A";
        private decimal TotalReturnAmount = 0;
        private decimal TotalReturnPercent = 0;
        private string TotalInvestedAmount = "N/A";

        // Chart data for Radzen
        private List<DataItem> PortfolioChartData = new();
        private List<DataItem> InvestedChartData = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            isLoading = false;
        }

        private async Task LoadData()
        {
            // Load available currencies from activities and account balances
            var activityCurrencies = await DbContext.Activities
                .Where(a => a is CashDepositWithdrawalActivity)
                .Cast<CashDepositWithdrawalActivity>()
                .Select(a => a.Amount.Currency.Symbol)
                .Distinct()
                .ToListAsync();

            var balanceCurrencies = await DbContext.Accounts
                .Include(a => a.Balance)
                .SelectMany(a => a.Balance)
                .Select(b => b.Money.Currency.Symbol)
                .Distinct()
                .ToListAsync();

            AvailableCurrencies = activityCurrencies.Union(balanceCurrencies)
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => c)
                .ToList();

            if (!AvailableCurrencies.Any())
            {
                AvailableCurrencies = ["USD", "EUR"];
            }

            selectedCurrency = AvailableCurrencies.FirstOrDefault() ?? "USD";

            await LoadPortfolioData();
        }

        private async Task LoadPortfolioData()
        {
            var endDate = DateTime.Today;
            var startDate = GetStartDateFromTimeframe(selectedTimeframe);

            // Load cash flows (deposits and withdrawals)
            var cashFlows = await LoadCashFlows(startDate, endDate);
            
            // Load account balances over time
            var accountBalances = await LoadAccountBalances(startDate, endDate);
            
            // Load holdings values over time (simplified for now)
            var holdingsValues = new List<HoldingValuePoint>();

            // Combine all data to calculate portfolio value over time
            PortfolioData = CalculatePortfolioValueOverTime(cashFlows, accountBalances, holdingsValues, startDate, endDate);

            // Prepare chart data
            PrepareChartData();

            // Calculate summary statistics
            CalculateSummaryStatistics();

            // Load portfolio breakdown by account
            await LoadPortfolioBreakdown();
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

        private async Task<List<CashFlowPoint>> LoadCashFlows(DateTime startDate, DateTime endDate)
        {
            var cashDeposits = await DbContext.Activities
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

            return cashDeposits.Where(cf => cf.Currency == selectedCurrency).ToList();
        }

        private async Task<List<BalancePoint>> LoadAccountBalances(DateTime startDate, DateTime endDate)
        {
            var balances = await DbContext.Accounts
                .Include(a => a.Balance)
                .SelectMany(a => a.Balance)
                .Where(b => b.Date >= DateOnly.FromDateTime(startDate) && 
                           b.Date <= DateOnly.FromDateTime(endDate) &&
                           b.Money.Currency.Symbol == selectedCurrency)
                .OrderBy(b => b.Date)
                .Select(b => new BalancePoint
                {
                    Date = b.Date.ToDateTime(TimeOnly.MinValue),
                    Amount = b.Money.Amount,
                    AccountId = b.AccountId
                })
                .ToListAsync();

            return balances;
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

            decimal cumulativeCashFlow = 0;
            decimal currentPortfolioValue = 0;

            foreach (var date in allDates)
            {
                // Update cumulative cash flow
                var cashFlowsUpToDate = cashFlows.Where(cf => cf.Date <= date).Sum(cf => cf.Amount);
                cumulativeCashFlow = cashFlowsUpToDate;

                // Get latest account balances up to this date
                var latestBalances = accountBalances
                    .Where(ab => ab.Date <= date)
                    .GroupBy(ab => ab.AccountId)
                    .Select(g => g.OrderByDescending(ab => ab.Date).First())
                    .Sum(ab => ab.Amount);

                currentPortfolioValue = latestBalances;

                portfolioPoints.Add(new PortfolioValuePoint
                {
                    Date = date,
                    TotalValue = currentPortfolioValue,
                    CashValue = latestBalances,
                    HoldingsValue = 0,
                    CumulativeInvested = cumulativeCashFlow
                });
            }

            return portfolioPoints.Where(p => p.Date >= startDate).ToList();
        }

        private void PrepareChartData()
        {
            if (PortfolioData == null || !PortfolioData.Any())
            {
                PortfolioChartData = new();
                InvestedChartData = new();
                return;
            }

            var sortedData = PortfolioData.OrderBy(p => p.Date).ToList();

            PortfolioChartData = sortedData.Select(p => new DataItem 
            { 
                Date = p.Date, 
                Value = (double)p.TotalValue 
            }).ToList();

            InvestedChartData = sortedData.Select(p => new DataItem 
            { 
                Date = p.Date, 
                Value = (double)p.CumulativeInvested 
            }).ToList();
        }

        private void CalculateSummaryStatistics()
        {
            if (PortfolioData == null || !PortfolioData.Any())
            {
                return;
            }

            var latestPoint = PortfolioData.OrderByDescending(p => p.Date).First();

            CurrentPortfolioValue = $"{latestPoint.TotalValue:N2} {selectedCurrency}";
            CurrentValueDate = latestPoint.Date.ToString("MMM dd, yyyy");
            TotalInvestedAmount = $"{latestPoint.CumulativeInvested:N2} {selectedCurrency}";

            TotalReturnAmount = latestPoint.TotalValue - latestPoint.CumulativeInvested;
            
            if (latestPoint.CumulativeInvested != 0)
            {
                TotalReturnPercent = (TotalReturnAmount / latestPoint.CumulativeInvested) * 100;
            }
        }

        private async Task LoadPortfolioBreakdown()
        {
            var accounts = await DbContext.Accounts
                .Include(a => a.Balance)
                .ToListAsync();

            var breakdown = new List<AccountBreakdown>();

            foreach (var account in accounts)
            {
                var latestBalance = account.Balance
                    .Where(b => b.Money.Currency.Symbol == selectedCurrency)
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

            PortfolioBreakdown = breakdown;
        }

        private async Task RefreshChart()
        {
            isLoading = true;
            StateHasChanged();

            await LoadPortfolioData();

            isLoading = false;
            StateHasChanged();
        }

        // Chart formatting methods
        private string FormatAsValue(object value)
        {
            if (double.TryParse(value?.ToString(), out double d))
            {
                return d.ToString("C0");
            }
            return value?.ToString() ?? "";
        }

        private string FormatAsDate(object value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("MMM dd");
            }
            return value?.ToString() ?? "";
        }

        // Data classes
        public class DataItem
        {
            public DateTime Date { get; set; }
            public double Value { get; set; }
        }

        private class PortfolioValuePoint
        {
            public DateTime Date { get; set; }
            public decimal TotalValue { get; set; }
            public decimal CashValue { get; set; }
            public decimal HoldingsValue { get; set; }
            public decimal CumulativeInvested { get; set; }
        }

        private class CashFlowPoint
        {
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }
            public string Currency { get; set; } = string.Empty;
            public bool IsDeposit { get; set; }
        }

        private class BalancePoint
        {
            public DateTime Date { get; set; }
            public decimal Amount { get; set; }
            public int AccountId { get; set; }
        }

        private class HoldingValuePoint
        {
            public DateTime Date { get; set; }
            public decimal Value { get; set; }
            public int HoldingId { get; set; }
            public decimal Quantity { get; set; }
            public decimal Price { get; set; }
        }

        private class AccountBreakdown
        {
            public string AccountName { get; set; } = string.Empty;
            public decimal CurrentValue { get; set; }
            public decimal CashBalance { get; set; }
            public decimal HoldingsValue { get; set; }
            public decimal PercentageOfPortfolio { get; set; }
        }
    }
}