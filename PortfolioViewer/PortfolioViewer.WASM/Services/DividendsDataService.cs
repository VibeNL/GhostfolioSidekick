using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Activities.Types.MoneyLists;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public class DividendsDataService(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IDividendsDataService
    {
        public async Task<List<DividendAggregateDisplayModel>> GetMonthlyDividendsAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId = 0,
            string symbol = "",
            string assetClass = "",
            CancellationToken cancellationToken = default)
        {
            var dividends = await GetDividendsAsync(targetCurrency, startDate, endDate, accountId, symbol, assetClass, cancellationToken);

            return dividends
                .GroupBy(d => new { Year = d.Date.Year, Month = d.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new DividendAggregateDisplayModel
                {
                    Period = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    TotalAmount = Money.Sum(g.Select(d => d.Amount)),
                    TotalTaxAmount = Money.Sum(g.Select(d => d.TaxAmount)),
                    TotalNetAmount = Money.Sum(g.Select(d => d.NetAmount)),
                    DividendCount = g.Count(),
                    Dividends = g.ToList()
                })
                .ToList();
        }

        public async Task<List<DividendAggregateDisplayModel>> GetYearlyDividendsAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId = 0,
            string symbol = "",
            string assetClass = "",
            CancellationToken cancellationToken = default)
        {
            var dividends = await GetDividendsAsync(targetCurrency, startDate, endDate, accountId, symbol, assetClass, cancellationToken);

            return dividends
                .GroupBy(d => d.Date.Year)
                .OrderBy(g => g.Key)
                .Select(g => new DividendAggregateDisplayModel
                {
                    Period = g.Key.ToString(),
                    Date = new DateTime(g.Key, 1, 1),
                    TotalAmount = Money.Sum(g.Select(d => d.Amount)),
                    TotalTaxAmount = Money.Sum(g.Select(d => d.TaxAmount)),
                    TotalNetAmount = Money.Sum(g.Select(d => d.NetAmount)),
                    DividendCount = g.Count(),
                    Dividends = g.ToList()
                })
                .ToList();
        }

        public async Task<List<DividendDisplayModel>> GetDividendsAsync(
            Currency targetCurrency,
            DateTime startDate,
            DateTime endDate,
            int accountId = 0,
            string symbol = "",
            string assetClass = "",
            CancellationToken cancellationToken = default)
        {
            var query = databaseContext.Activities
                .OfType<DividendActivity>()
                .Where(d => d.Date >= startDate && d.Date <= endDate);

            if (accountId > 0)
            {
                query = query.Where(d => d.Account.Id == accountId);
            }

            if (!string.IsNullOrEmpty(symbol))
            {
                query = query.Where(d => d.Holding != null && d.Holding.SymbolProfiles.Any(sp => sp.Symbol == symbol));
            }

            if (!string.IsNullOrEmpty(assetClass))
            {
                query = query.Where(d => d.Holding != null && d.Holding.SymbolProfiles.Any(sp => sp.AssetClass.ToString() == assetClass));
            }

            var dividendActivities = await query
                .Include(d => d.Account)
                .Include(d => d.Holding)
                .ThenInclude(h => h!.SymbolProfiles)
                .Include(d => d.Taxes)
                .ToListAsync(cancellationToken);

            var result = new List<DividendDisplayModel>();

            foreach (var dividend in dividendActivities)
            {
                var convertedAmount = await currencyExchange.ConvertMoney(dividend.Amount, targetCurrency, DateOnly.FromDateTime(dividend.Date));
                
                var totalTaxAmount = Money.Zero(targetCurrency);
                if (dividend.Taxes.Any())
                {
                    var taxAmounts = new List<Money>();
                    foreach (var tax in dividend.Taxes)
                    {
                        var convertedTax = await currencyExchange.ConvertMoney(tax.Money, targetCurrency, DateOnly.FromDateTime(dividend.Date));
                        taxAmounts.Add(convertedTax);
                    }
                    totalTaxAmount = Money.Sum(taxAmounts);
                }

                var netAmount = convertedAmount.Subtract(totalTaxAmount);

                var symbolProfile = dividend.Holding?.SymbolProfiles?.FirstOrDefault();

                result.Add(new DividendDisplayModel
                {
                    Symbol = symbolProfile?.Symbol ?? string.Empty,
                    Name = symbolProfile?.Name ?? string.Empty,
                    Date = dividend.Date,
                    Amount = convertedAmount,
                    TaxAmount = totalTaxAmount,
                    NetAmount = netAmount,
                    AssetClass = symbolProfile?.AssetClass.ToString() ?? string.Empty,
                    Sector = symbolProfile?.SectorWeights?.FirstOrDefault()?.Name ?? string.Empty,
                    AccountName = dividend.Account.Name
                });
            }

            return result.OrderByDescending(d => d.Date).ToList();
        }

        public async Task<List<Account>> GetAccountsAsync()
        {
            return await databaseContext.Accounts.ToListAsync();
        }

        public async Task<List<string>> GetDividendSymbolsAsync()
        {
            return await databaseContext.Activities
                .OfType<DividendActivity>()
                .Where(d => d.Holding != null && d.Holding.SymbolProfiles.Any())
                .SelectMany(d => d.Holding!.SymbolProfiles.Select(sp => sp.Symbol))
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        public async Task<List<string>> GetDividendAssetClassesAsync()
        {
            return await databaseContext.Activities
                .OfType<DividendActivity>()
                .Where(d => d.Holding != null && d.Holding.SymbolProfiles.Any())
                .SelectMany(d => d.Holding!.SymbolProfiles.Select(sp => sp.AssetClass.ToString()))
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        public async Task<DateOnly> GetMinDividendDateAsync()
        {
            var minDate = await databaseContext.Activities
                .OfType<DividendActivity>()
                .OrderBy(d => d.Date)
                .Select(d => d.Date)
                .FirstOrDefaultAsync();

            return minDate != default ? DateOnly.FromDateTime(minDate) : DateOnly.FromDateTime(DateTime.Today.AddYears(-10));
        }
    }
}