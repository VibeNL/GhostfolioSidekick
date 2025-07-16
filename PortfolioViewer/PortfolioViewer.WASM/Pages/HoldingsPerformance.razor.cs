using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class HoldingsPerformance : IDisposable
    {
        [Inject]
        private IHoldingsPerformanceService HoldingsPerformanceService { get; set; } = default!;

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
                // Load holdings data with pagination
                var (holdings, totalCount) = await HoldingsPerformanceService.GetHoldingsDataAsync(PageSize, currentPage);
                AllHoldings = holdings;
                FilteredHoldings = holdings.OrderBy(h => h.Symbol).ToList();
                totalHoldings = totalCount;

                // Load supporting data
                await LoadSupportingData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading holdings data: {ex.Message}");
                AllHoldings = new List<HoldingPerformanceData>();
                FilteredHoldings = new List<HoldingPerformanceData>();
                totalHoldings = 0;
                await LoadSupportingDataSafely();
            }
        }

        private async Task LoadSupportingData()
        {
            try
            {
                var tasks = new[]
                {
                    LoadAssetClassesAsync(),
                    LoadAssetClassDistributionAsync(),
                    LoadMostActiveHoldingsAsync()
                };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading supporting data: {ex.Message}");
                await LoadSupportingDataSafely();
            }
        }

        private async Task LoadSupportingDataSafely()
        {
            // Load data safely with individual try-catch blocks
            try
            {
                AssetClasses = await HoldingsPerformanceService.GetAssetClassesAsync();
            }
            catch { AssetClasses = new List<string>(); }

            try
            {
                AssetClassDistribution = await HoldingsPerformanceService.GetAssetClassDistributionAsync();
            }
            catch { AssetClassDistribution = new List<AssetClassDistributionItem>(); }

            try
            {
                MostActiveHoldings = await HoldingsPerformanceService.GetMostActiveHoldingsAsync();
            }
            catch { MostActiveHoldings = new List<ActiveHoldingItem>(); }
        }

        private async Task LoadAssetClassesAsync()
        {
            AssetClasses = await HoldingsPerformanceService.GetAssetClassesAsync();
        }

        private async Task LoadAssetClassDistributionAsync()
        {
            AssetClassDistribution = await HoldingsPerformanceService.GetAssetClassDistributionAsync();
        }

        private async Task LoadMostActiveHoldingsAsync()
        {
            MostActiveHoldings = await HoldingsPerformanceService.GetMostActiveHoldingsAsync();
        }

        private async Task FilterHoldings()
        {
            try
            {
                FilteredHoldings = await HoldingsPerformanceService.FilterHoldingsByAssetClassAsync(selectedAssetClass);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filtering holdings: {ex.Message}");
                FilteredHoldings = AllHoldings?.Where(h => 
                    string.IsNullOrEmpty(selectedAssetClass) || h.AssetClass == selectedAssetClass)
                    .OrderBy(h => h.Symbol)
                    .ToList() ?? new List<HoldingPerformanceData>();
                StateHasChanged();
            }
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
    }
}