using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.Services.Implementation;

public class HoldingsPerformanceService : IHoldingsPerformanceService
{
    private readonly DatabaseContext _dbContext;
    private readonly ILogger<HoldingsPerformanceService> _logger;
    private const int DefaultPageSize = 100;

    public HoldingsPerformanceService(DatabaseContext dbContext, ILogger<HoldingsPerformanceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(List<HoldingPerformanceData> holdings, int totalCount)> GetHoldingsDataAsync(int pageSize = DefaultPageSize, int page = 1)
    {
        try
        {
            var totalCount = await _dbContext.Holdings.CountAsync(h => h.SymbolProfiles.Any());
            
            // Get holdings IDs with pagination
            var holdingIds = await _dbContext.Holdings
                .Where(h => h.SymbolProfiles.Any())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => h.Id)
                .ToListAsync();

            var holdings = await ProcessHoldingsBatch(holdingIds);
            
            return (holdings, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading holdings data for page {Page} with page size {PageSize}", page, pageSize);
            return (new List<HoldingPerformanceData>(), 0);
        }
    }

    public async Task<List<string>> GetAssetClassesAsync()
    {
        try
        {
            return await _dbContext.SymbolProfiles
                .Select(sp => sp.AssetClass.ToString())
                .Distinct()
                .OrderBy(ac => ac)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading asset classes");
            return new List<string>();
        }
    }

    public async Task<List<AssetClassDistributionItem>> GetAssetClassDistributionAsync()
    {
        try
        {
            var holdings = await _dbContext.Holdings
                .Include(h => h.SymbolProfiles)
                .Where(h => h.SymbolProfiles.Any())
                .ToListAsync();

            var totalCount = holdings.Count;
            if (totalCount == 0) return new List<AssetClassDistributionItem>();

            return holdings
                .SelectMany(h => h.SymbolProfiles)
                .GroupBy(sp => sp.AssetClass.ToString())
                .Select(g => new AssetClassDistributionItem
                {
                    AssetClass = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / totalCount * 100
                })
                .OrderByDescending(item => item.Count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating asset class distribution");
            return new List<AssetClassDistributionItem>();
        }
    }

    public async Task<List<ActiveHoldingItem>> GetMostActiveHoldingsAsync(int count = 10)
    {
        try
        {
            return await _dbContext.Holdings
                .AsNoTracking()
                .Where(h => h.Activities.Any())
                .OrderByDescending(h => h.Activities.Count())
                .Take(count)
                .Select(h => new ActiveHoldingItem
                {
                    Symbol = h.SymbolProfiles.FirstOrDefault()!.Symbol,
                    ActivityCount = h.Activities.Count(),
                    LatestActivityDate = h.Activities.Max(a => a.Date)
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading most active holdings");
            return new List<ActiveHoldingItem>();
        }
    }

    public async Task<List<HoldingPerformanceData>> FilterHoldingsByAssetClassAsync(string? assetClass = null)
    {
        try
        {
            var query = _dbContext.Holdings
                .AsNoTracking()
                .Include(h => h.SymbolProfiles)
                .Where(h => h.SymbolProfiles.Any());

            if (!string.IsNullOrEmpty(assetClass))
            {
                query = query.Where(h => h.SymbolProfiles.Any(sp => sp.AssetClass.ToString() == assetClass));
            }

            var holdings = await query.ToListAsync();
            var result = new List<HoldingPerformanceData>();

            foreach (var holding in holdings)
            {
                var symbolProfile = holding.SymbolProfiles.FirstOrDefault();
                if (symbolProfile == null) continue;

                // Load activity count
                var activityCount = await _dbContext.Activities
                    .AsNoTracking()
                    .CountAsync(a => a.Holding != null && a.Holding.Id == holding.Id);

                result.Add(new HoldingPerformanceData
                {
                    Symbol = symbolProfile.Symbol,
                    Name = symbolProfile.Name ?? "Unknown",
                    AssetClass = symbolProfile.AssetClass.ToString(),
                    AssetSubClass = symbolProfile.AssetSubClass?.ToString(),
                    DataSource = symbolProfile.DataSource,
                    ISIN = symbolProfile.ISIN,
                    ActivityCount = activityCount,
                    MarketDataCount = 0, // Simplified for performance
                    LatestPrice = "N/A",
                    LatestPriceDate = null,
                    TotalQuantity = 0 // Simplified for performance
                });
            }

            return result.OrderBy(h => h.Symbol).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering holdings by asset class {AssetClass}", assetClass);
            return new List<HoldingPerformanceData>();
        }
    }

    private async Task<List<HoldingPerformanceData>> ProcessHoldingsBatch(List<int> holdingIds)
    {
        var results = new List<HoldingPerformanceData>();

        var holdingsWithBasicData = await _dbContext.Holdings
            .AsNoTracking()
            .Where(h => holdingIds.Contains(h.Id))
            .Select(h => new
            {
                h.Id,
                SymbolProfile = h.SymbolProfiles.FirstOrDefault(),
                ActivityCount = h.Activities.Count()
            })
            .ToListAsync();

        foreach (var holding in holdingsWithBasicData)
        {
            if (holding.SymbolProfile == null) continue;

            // Load market data separately to get only latest
            var latestMarketData = await _dbContext.MarketDatas
                .AsNoTracking()
                .Where(md => EF.Property<string>(md, "SymbolProfileSymbol") == holding.SymbolProfile.Symbol &&
                            EF.Property<string>(md, "SymbolProfileDataSource") == holding.SymbolProfile.DataSource)
                .OrderByDescending(md => md.Date)
                .Select(md => new { md.Close, md.Date })
                .FirstOrDefaultAsync();

            // Calculate total quantity
            var totalQuantity = await _dbContext.Activities
                .AsNoTracking()
                .Where(a => a.Holding != null && a.Holding.Id == holding.Id)
                .OfType<ActivityWithQuantityAndUnitPrice>()
                .SumAsync(a => a.Quantity);

            // Get market data count
            var marketDataCount = await _dbContext.MarketDatas
                .AsNoTracking()
                .CountAsync(md => EF.Property<string>(md, "SymbolProfileSymbol") == holding.SymbolProfile.Symbol &&
                                 EF.Property<string>(md, "SymbolProfileDataSource") == holding.SymbolProfile.DataSource);

            results.Add(new HoldingPerformanceData
            {
                Symbol = holding.SymbolProfile.Symbol,
                Name = holding.SymbolProfile.Name ?? "Unknown",
                AssetClass = holding.SymbolProfile.AssetClass.ToString(),
                AssetSubClass = holding.SymbolProfile.AssetSubClass?.ToString(),
                DataSource = holding.SymbolProfile.DataSource,
                ISIN = holding.SymbolProfile.ISIN,
                ActivityCount = holding.ActivityCount,
                MarketDataCount = marketDataCount,
                LatestPrice = latestMarketData?.Close.ToString(),
                LatestPriceDate = latestMarketData?.Date,
                TotalQuantity = totalQuantity
            });
        }

        return results;
    }
}