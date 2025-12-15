using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
    public class UpcomingDividendsService(IDbContextFactory<DatabaseContext> dbContextFactory) : IUpcomingDividendsService
    {
        public async Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
        {
            await using var databaseContext = await dbContextFactory.CreateDbContextAsync();

            // Get all holdings (symbol, quantity) - handle nulls safely
            var holdings = await databaseContext.HoldingAggregateds
                .Select(h => new {
                    h.Symbol,
                    Quantity = h.CalculatedSnapshotsPrimaryCurrency
                        .OrderByDescending(s => s.Date)
                        .Select(s => (decimal?)s.Quantity)
                        .FirstOrDefault() ?? 0
                })
                .ToListAsync();

            // Build a dictionary for quick lookup
            var holdingsDict = holdings
                .Where(h => h.Quantity > 0)
                .GroupBy(h => h.Symbol)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // Get upcoming dividends, join with SymbolProfiles using explicit properties
            var today = DateOnly.FromDateTime(DateTime.Today);
            var dividends = await databaseContext.Dividends
                .Where(dividend => dividend.ExDividendDate >= today)
                .Join(databaseContext.SymbolProfiles,
                    dividend => new { Symbol = dividend.SymbolProfileSymbol, DataSource = dividend.SymbolProfileDataSource },
                    symbolProfile => new { Symbol = (string?)symbolProfile.Symbol, DataSource = (string?)symbolProfile.DataSource },
                    (dividend, symbolProfile) => new { Dividend = dividend, SymbolProfile = symbolProfile })
                .Where(x => x.Dividend.Amount.Amount > 0)
                .ToListAsync();

            var result = new List<UpcomingDividendModel>();
            foreach (var item in dividends)
            {
                var symbol = item.SymbolProfile.Symbol ?? string.Empty;
                var companyName = item.SymbolProfile.Name ?? string.Empty;
                holdingsDict.TryGetValue(symbol, out var quantity);
                var dividendPerShare = item.Dividend.Amount.Amount;
                var expectedAmount = dividendPerShare * quantity;

                if (expectedAmount <= 0)
                {
                    continue;
                }

                result.Add(new UpcomingDividendModel
                {
                    Symbol = symbol,
                    CompanyName = companyName,
                    ExDate = item.Dividend.ExDividendDate.ToDateTime(TimeOnly.MinValue),
                    PaymentDate = item.Dividend.PaymentDate.ToDateTime(TimeOnly.MinValue),
                    Amount = expectedAmount,
                    Currency = item.Dividend.Amount.Currency.Symbol,
                    Quantity = quantity,
                    DividendPerShare = dividendPerShare
                });
            }

            return result;
        }
    }
}