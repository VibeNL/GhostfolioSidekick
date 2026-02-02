using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
    public class UpcomingDividendsService(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        ICurrencyExchange currencyExchange,
        IServerConfigurationService serverConfigurationService) : IUpcomingDividendsService
    {
        public async Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
        {
            await using var databaseContext = await dbContextFactory.CreateDbContextAsync();

            // Get the primary currency to convert all amounts to
            var primaryCurrency = await serverConfigurationService.GetPrimaryCurrencyAsync();
            
            // Get the latest date for calculated snapshots
            var lastKnownDate = await databaseContext.CalculatedSnapshots
                .MaxAsync(x => (DateOnly?)x.Date);

            if (lastKnownDate == null)
            {
                return [];
            }

            // Get all holdings quantities summed across all accounts for the latest date
            // Flatten the many-to-many relationship between Holdings and SymbolProfiles
            var holdings = await databaseContext.Holdings
                .SelectMany(h => h.SymbolProfiles.Select(sp => new
                {
                    Symbol = sp.Symbol,
                    Quantity = h.CalculatedSnapshots
                        .Where(s => s.Date == lastKnownDate)
                        .Sum(s => s.Quantity)
                }))
                .Where(h => h.Quantity > 0)
                .ToListAsync();

            // Build a dictionary for quick lookup
            var holdingsDict = holdings
                .GroupBy(h => h.Symbol)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // Get upcoming dividends, join with SymbolProfiles using explicit properties
            var today = DateOnly.FromDateTime(DateTime.Today);
            var dividends = await databaseContext.Dividends
                .Where(dividend => dividend.PaymentDate >= today)
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

                if (quantity <= 0)
                {
                    continue;
                }

                // Native currency values (original dividend currency)
                var dividendPerShare = item.Dividend.Amount.Amount;
                var expectedAmount = dividendPerShare * quantity;
                var nativeCurrency = item.Dividend.Amount.Currency.Symbol;

                // Convert dividend per share to primary currency
                var dividendPerShareConverted = await currencyExchange.ConvertMoney(
                    item.Dividend.Amount, 
                    primaryCurrency, 
                    item.Dividend.ExDividendDate);
                
                var dividendPerSharePrimaryCurrency = dividendPerShareConverted.Amount;
                var expectedAmountPrimaryCurrency = dividendPerSharePrimaryCurrency * quantity;

                result.Add(new UpcomingDividendModel
                {
                    Symbol = symbol,
                    CompanyName = companyName,
                    ExDate = item.Dividend.ExDividendDate.ToDateTime(TimeOnly.MinValue),
                    PaymentDate = item.Dividend.PaymentDate.ToDateTime(TimeOnly.MinValue),
                    
                    // Native currency (original dividend currency)
                    Amount = expectedAmount,
                    Currency = nativeCurrency,
                    DividendPerShare = dividendPerShare,
                    
                    // Primary currency equivalent
                    AmountPrimaryCurrency = expectedAmountPrimaryCurrency,
                    PrimaryCurrency = primaryCurrency.Symbol,
                    DividendPerSharePrimaryCurrency = dividendPerSharePrimaryCurrency,
                    
                    Quantity = quantity
                });
            }

            return result;
        }
    }
}