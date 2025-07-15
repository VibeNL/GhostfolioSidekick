using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class HoldingsPerformance
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

        protected override async Task OnInitializedAsync()
        {
            await LoadHoldingsData();
            isLoading = false;
        }

        private async Task LoadHoldingsData()
        {
            var holdings = await DbContext.Holdings
                .Include(h => h.SymbolProfiles)
                    .ThenInclude(sp => sp.MarketData)
                .Include(h => h.Activities)
                .ToListAsync();

            AllHoldings = new List<HoldingPerformanceData>();

            foreach (var holding in holdings.Where(h => h.SymbolProfiles.Any()))
            {
                var symbolProfile = holding.SymbolProfiles.First();
                
                // Get the latest market data from the navigation property
                var marketData = symbolProfile.MarketData
                    .OrderByDescending(md => md.Date)
                    .FirstOrDefault();

                // Calculate total quantity from activities
                var quantityActivities = holding.Activities.OfType<ActivityWithQuantityAndUnitPrice>();
                var totalQuantity = quantityActivities.Sum(a => a.Quantity);

                AllHoldings.Add(new HoldingPerformanceData
                {
                    Symbol = symbolProfile.Symbol,
                    Name = symbolProfile.Name ?? "Unknown",
                    AssetClass = symbolProfile.AssetClass.ToString(),
                    AssetSubClass = symbolProfile.AssetSubClass?.ToString(),
                    DataSource = symbolProfile.DataSource,
                    ISIN = symbolProfile.ISIN,
                    ActivityCount = holding.Activities.Count,
                    MarketDataCount = symbolProfile.MarketData.Count,
                    LatestPrice = marketData?.Close.ToString(),
                    LatestPriceDate = marketData?.Date,
                    TotalQuantity = totalQuantity
                });
            }

            AssetClasses = AllHoldings.Select(h => h.AssetClass).Distinct().OrderBy(ac => ac).ToList();
            FilteredHoldings = AllHoldings.OrderBy(h => h.Symbol).ToList();

            // Calculate asset class distribution
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

            // Get most active holdings
            MostActiveHoldings = AllHoldings
                .Where(h => h.ActivityCount > 0)
                .OrderByDescending(h => h.ActivityCount)
                .Take(10)
                .Select(h => new ActiveHoldingItem
                {
                    Symbol = h.Symbol,
                    ActivityCount = h.ActivityCount,
                    LatestActivityDate = holdings.First(hld => hld.SymbolProfiles.Any(sp => sp.Symbol == h.Symbol))
                        .Activities.Max(a => a.Date)
                })
                .ToList();
        }

        private async Task FilterHoldings()
        {
            if (string.IsNullOrEmpty(selectedAssetClass))
            {
                FilteredHoldings = AllHoldings?.OrderBy(h => h.Symbol).ToList();
            }
            else
            {
                FilteredHoldings = AllHoldings?
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