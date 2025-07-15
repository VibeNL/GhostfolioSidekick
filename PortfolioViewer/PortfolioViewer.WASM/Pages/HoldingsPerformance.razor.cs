using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class HoldingsPerformance : IDisposable
    {
        [Inject]
        private DatabaseContext DbContext { get; set; } = default!;

        private bool isLoading = true;
        private string selectedAssetClass = "";
        private List<HoldingPerformanceData>? AllHoldings;
        private List<HoldingPerformanceData>? FilteredHoldings;
        private List<string>? AssetClasses;
        private List<AssetClassDistributionItem>? AssetClassDistribution;
        private List<ActiveHoldingItem>? MostActiveHoldings;
        private HoldingDetailsData? SelectedHoldingDetails;

        // Pagination for memory efficiency
        private const int PageSize = 100;
        private int currentPage = 1;
        private int totalHoldings = 0;
        private bool disposed = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadHoldingsData();
            isLoading = false;
        }

        private async Task LoadHoldingsData()
        {
            try
            {
                // First, get total count and asset classes without loading full data
                totalHoldings = await DbContext.Holdings.CountAsync(h => h.SymbolProfiles.Any());
                
                AssetClasses = await DbContext.SymbolProfiles
                    .Select(sp => sp.AssetClass.ToString())
                    .Distinct()
                    .OrderBy(ac => ac)
                    .ToListAsync();

                // Load holdings data in batches using streaming approach
                AllHoldings = await LoadHoldingsBatch();
                FilteredHoldings = AllHoldings.OrderBy(h => h.Symbol).ToList();

                // Load analytics data efficiently
                await LoadAnalyticsData();
            }
            catch (OutOfMemoryException)
            {
                // Fallback to even more conservative approach
                await LoadHoldingsDataConservative();
            }
            catch (Exception ex) when (ex.Message.Contains("out of memory") || ex.Message.Contains("OutOfMemory"))
            {
                // Catch other memory-related exceptions
                await LoadHoldingsDataConservative();
            }
        }

        private async Task<List<HoldingPerformanceData>> LoadHoldingsBatch()
        {
            var holdingsData = new List<HoldingPerformanceData>();
            
            // Get holdings IDs first (minimal memory footprint)
            var holdingIds = await DbContext.Holdings
                .Where(h => h.SymbolProfiles.Any())
                .Select(h => h.Id)
                .ToListAsync();

            // Process holdings in small batches to avoid memory issues
            const int batchSize = 25; // Reduced batch size for better memory management
            
            for (int i = 0; i < holdingIds.Count; i += batchSize)
            {
                // Check if component is disposed
                if (disposed) break;

                var batchIds = holdingIds.Skip(i).Take(batchSize).ToList();
                var batchData = await ProcessHoldingsBatch(batchIds);
                holdingsData.AddRange(batchData);
                
                // Force garbage collection more frequently for memory-constrained environments
                if (i % (batchSize * 2) == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect(); // Second collection to clean up finalized objects
                }

                // Give UI thread a chance to update (for progress indication if needed)
                await Task.Delay(1);
            }

            return holdingsData;
        }

        private async Task<List<HoldingPerformanceData>> ProcessHoldingsBatch(List<int> holdingIds)
        {
            var results = new List<HoldingPerformanceData>();

            // Use AsNoTracking for better memory efficiency
            var holdingsWithBasicData = await DbContext.Holdings
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

                // Load market data separately and get only latest using the correct navigation
                var latestMarketData = await DbContext.MarketDatas
                    .AsNoTracking()
                    .Where(md => EF.Property<string>(md, "SymbolProfileSymbol") == holding.SymbolProfile.Symbol &&
                                EF.Property<string>(md, "SymbolProfileDataSource") == holding.SymbolProfile.DataSource)
                    .OrderByDescending(md => md.Date)
                    .Select(md => new { md.Close, md.Date })
                    .FirstOrDefaultAsync();

                // Calculate total quantity using database aggregation instead of loading all activities
                var totalQuantity = await DbContext.Activities
                    .AsNoTracking()
                    .Where(a => a.Holding != null && a.Holding.Id == holding.Id)
                    .OfType<ActivityWithQuantityAndUnitPrice>()
                    .SumAsync(a => a.Quantity);

                // Get market data count efficiently
                var marketDataCount = await DbContext.MarketDatas
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

        private async Task LoadAnalyticsData()
        {
            if (AllHoldings == null || !AllHoldings.Any()) return;

            // Calculate asset class distribution from already loaded data
            AssetClassDistribution = AllHoldings
                .GroupBy(h => h.AssetClass)
                .Select(g => new AssetClassDistributionItem
                {
                    AssetClass = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / AllHoldings.Count * 100
                })
                .OrderByDescending(item => item.Count)
                .ToList();

            // Get most active holdings efficiently using database query
            var topActiveSymbols = await DbContext.Holdings
                .AsNoTracking()
                .Where(h => h.Activities.Any())
                .OrderByDescending(h => h.Activities.Count())
                .Take(10)
                .Select(h => new
                {
                    Symbol = h.SymbolProfiles.FirstOrDefault()!.Symbol,
                    ActivityCount = h.Activities.Count(),
                    LatestActivityDate = h.Activities.Max(a => a.Date)
                })
                .ToListAsync();

            MostActiveHoldings = topActiveSymbols
                .Select(h => new ActiveHoldingItem
                {
                    Symbol = h.Symbol,
                    ActivityCount = h.ActivityCount,
                    LatestActivityDate = h.LatestActivityDate
                })
                .ToList();
        }

        private async Task LoadHoldingsDataConservative()
        {
            // Ultra-conservative approach for systems with severe memory constraints
            var holdingCount = Math.Min(25, totalHoldings); // Even more conservative limit
            
            var limitedHoldings = await DbContext.Holdings
                .AsNoTracking()
                .Where(h => h.SymbolProfiles.Any())
                .Take(holdingCount)
                .Select(h => new
                {
                    h.Id,
                    Symbol = h.SymbolProfiles.FirstOrDefault()!.Symbol,
                    Name = h.SymbolProfiles.FirstOrDefault()!.Name,
                    AssetClass = h.SymbolProfiles.FirstOrDefault()!.AssetClass.ToString(),
                    AssetSubClass = h.SymbolProfiles.FirstOrDefault()!.AssetSubClass!.ToString(),
                    DataSource = h.SymbolProfiles.FirstOrDefault()!.DataSource,
                    ISIN = h.SymbolProfiles.FirstOrDefault()!.ISIN,
                    ActivityCount = h.Activities.Count()
                })
                .ToListAsync();

            AllHoldings = limitedHoldings.Select(h => new HoldingPerformanceData
            {
                Symbol = h.Symbol,
                Name = h.Name ?? "Unknown",
                AssetClass = h.AssetClass,
                AssetSubClass = h.AssetSubClass,
                DataSource = h.DataSource,
                ISIN = h.ISIN,
                ActivityCount = h.ActivityCount,
                MarketDataCount = 0, // Skip market data for memory conservation
                LatestPrice = "N/A",
                LatestPriceDate = null,
                TotalQuantity = 0 // Skip quantity calculation for memory conservation
            }).ToList();

            FilteredHoldings = AllHoldings.OrderBy(h => h.Symbol).ToList();

            // Simple asset class distribution
            AssetClasses = AllHoldings.Select(h => h.AssetClass).Distinct().OrderBy(ac => ac).ToList();
            AssetClassDistribution = AllHoldings
                .GroupBy(h => h.AssetClass)
                .Select(g => new AssetClassDistributionItem
                {
                    AssetClass = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / AllHoldings.Count * 100
                })
                .OrderByDescending(item => item.Count)
                .ToList();

            MostActiveHoldings = AllHoldings
                .OrderByDescending(h => h.ActivityCount)
                .Take(5)
                .Select(h => new ActiveHoldingItem
                {
                    Symbol = h.Symbol,
                    ActivityCount = h.ActivityCount,
                    LatestActivityDate = DateTime.MinValue
                })
                .ToList();
        }

        private async Task FilterHoldings()
        {
            if (AllHoldings == null) return;

            if (string.IsNullOrEmpty(selectedAssetClass))
            {
                FilteredHoldings = AllHoldings.OrderBy(h => h.Symbol).ToList();
            }
            else
            {
                FilteredHoldings = AllHoldings
                    .Where(h => h.AssetClass == selectedAssetClass)
                    .OrderBy(h => h.Symbol)
                    .ToList();
            }
            StateHasChanged();
        }

        private string GetAssetClassBadge(string assetClass)
        {
            return assetClass.ToLower() switch
            {
                "equity" => "bg-success",
                "cryptocurrency" => "bg-warning text-dark",
                "cash" => "bg-info",
                "commodity" => "bg-secondary",
                "fixedincome" => "bg-primary",
                _ => "bg-light text-dark"
            };
        }

        private string GetAssetClassProgressBar(string assetClass)
        {
            return assetClass.ToLower() switch
            {
                "equity" => "bg-success",
                "cryptocurrency" => "bg-warning",
                "cash" => "bg-info",
                "commodity" => "bg-secondary",
                "fixedincome" => "bg-primary",
                _ => "bg-light"
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                // Clear all collections to release memory
                AllHoldings?.Clear();
                FilteredHoldings?.Clear();
                AssetClasses?.Clear();
                AssetClassDistribution?.Clear();
                MostActiveHoldings?.Clear();
                
                // Set references to null
                AllHoldings = null;
                FilteredHoldings = null;
                AssetClasses = null;
                AssetClassDistribution = null;
                MostActiveHoldings = null;
                SelectedHoldingDetails = null;
                
                disposed = true;
            }
        }

        private class HoldingPerformanceData
        {
            public string Symbol { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string AssetClass { get; set; } = string.Empty;
            public string? AssetSubClass { get; set; }
            public string DataSource { get; set; } = string.Empty;
            public string? ISIN { get; set; }
            public int ActivityCount { get; set; }
            public int MarketDataCount { get; set; }
            public string? LatestPrice { get; set; }
            public DateOnly? LatestPriceDate { get; set; }
            public decimal TotalQuantity { get; set; }
        }

        private class AssetClassDistributionItem
        {
            public string AssetClass { get; set; } = string.Empty;
            public int Count { get; set; }
            public double Percentage { get; set; }
        }

        private class ActiveHoldingItem
        {
            public string Symbol { get; set; } = string.Empty;
            public int ActivityCount { get; set; }
            public DateTime? LatestActivityDate { get; set; }
        }

        private class HoldingDetailsData
        {
            public string Symbol { get; set; } = string.Empty;
            public List<ActivityItem> RecentActivities { get; set; } = [];
            public List<MarketDataItem> RecentMarketData { get; set; } = [];
        }

        private class ActivityItem
        {
            public DateTime Date { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Quantity { get; set; } = string.Empty;
            public string Price { get; set; } = string.Empty;
        }

        private class MarketDataItem
        {
            public DateOnly Date { get; set; }
            public string Close { get; set; } = string.Empty;
            public string High { get; set; } = string.Empty;
            public string Low { get; set; } = string.Empty;
        }
    }
}