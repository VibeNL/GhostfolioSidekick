using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class Holdings
    {
		[Inject]
		private IHoldingsDataService? HoldingsDataService { get; set; }
		// View mode for the treemap

		private string ViewMode = "treemap";
        private List<HoldingDisplayModel> HoldingsList = new();
        private PlotlyChart? treemapChart;
        private Config plotConfig = new();
        private Plotly.Blazor.Layout plotLayout = new();
        private IList<ITrace> plotData = new List<ITrace>();

        // Loading state management
        private bool IsLoading { get; set; } = true;
        private bool HasError { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadPortfolioDataAsync();
        }

        private async Task LoadPortfolioDataAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = string.Empty;
                StateHasChanged(); // Update UI to show loading state

                // Yield control to allow UI to update
                await Task.Yield();
                
                HoldingsList = await LoadRealPortfolioDataAsync();
                
                // Prepare chart data after loading
                await Task.Run(() => PrepareTreemapData());
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
                StateHasChanged(); // Update UI when loading is complete
            }
        }

		private async Task<List<HoldingDisplayModel>> LoadRealPortfolioDataAsync()
		{
			try
			{
				return await HoldingsDataService?.GetHoldingsAsync(Model.Currency.EUR) ?? new List<HoldingDisplayModel>();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to load portfolio data: {ex.Message}", ex);
			}
		}

		private void PrepareTreemapData()
        {
			if (HoldingsList.Count == 0)
			{
				return;
			}

            var treemapTrace = new TreeMap
            {
                Labels = HoldingsList.Select(h => $"{h.Name}({h.Symbol})").ToArray(),
                Values = HoldingsList.Select(h => (object)h.CurrentValue).ToList(),
                Parents = HoldingsList.Select(h => "").ToArray(),
                Text = HoldingsList.Select(h => $"{h.Symbol}<br>{h.Currency}{(h.CurrentValue / 1000):F0}k<br>{h.GainLossPercentage:P1}").ToArray(),
                TextInfo = Plotly.Blazor.Traces.TreeMapLib.TextInfoFlag.Text,
                BranchValues = Plotly.Blazor.Traces.TreeMapLib.BranchValuesEnum.Total,
                PathBar = new Plotly.Blazor.Traces.TreeMapLib.PathBar
                {
                    Visible = false
                },
                Marker = new Plotly.Blazor.Traces.TreeMapLib.Marker
                {
                    Colors = HoldingsList.Select(h => (object)GetColorForSector(h.Sector)).ToList(),
                    Line = new Plotly.Blazor.Traces.TreeMapLib.MarkerLib.Line
                    {
                        Width = 2,
                        Color = "#ffffff"
                    }
                },
                TextFont = new Plotly.Blazor.Traces.TreeMapLib.TextFont
                {
                    Size = 12,
                    Color = "#ffffff"
                }
            };

            plotData = new List<ITrace> { treemapTrace };

            plotLayout = new Plotly.Blazor.Layout
            {
                Title = new Plotly.Blazor.LayoutLib.Title
                {
                    Text = "Portfolio Allocation"
                },
                Margin = new Plotly.Blazor.LayoutLib.Margin
                {
                    T = 50,
                    L = 10,
                    R = 10,
                    B = 10
                },
                Height = 450
            };

            plotConfig = new Config
            {
                Responsive = true
            };
        }

        private string GetColorForSector(string sector)
        {
            var colors = new Dictionary<string, string>
            {
                { "Technology", "#3498db" },
                { "Healthcare", "#e74c3c" },
                { "Financial Services", "#2ecc71" },
                { "Consumer Cyclical", "#f39c12" },
                { "Communication Services", "#9b59b6" },
                { "Industrial", "#1abc9c" },
                { "Consumer Defensive", "#95a5a6" },
                { "Energy", "#e67e22" },
                { "Utilities", "#34495e" },
                { "Real Estate", "#8e44ad" },
                { "Basic Materials", "#d35400" },
                { "Cryptocurrency", "#f1c40f" }
            };

            return colors.TryGetValue(sector, out var color) ? color : "#7f8c8d";
        }

        // Add refresh method for manual data reload
        private async Task RefreshDataAsync()
        {
            await LoadPortfolioDataAsync();
        }

        private decimal TotalValue => HoldingsList.Sum(h => h.CurrentValue);
        private decimal TotalGainLoss => HoldingsList.Sum(h => h.GainLoss);
        private decimal TotalGainLossPercentage => TotalValue > 0 ? TotalGainLoss / (TotalValue - TotalGainLoss) : 0;

        private Dictionary<string, decimal> SectorAllocation =>
            HoldingsList.GroupBy(h => h.Sector)
                   .ToDictionary(g => g.Key, g => g.Sum(h => h.Weight));

        private Dictionary<string, decimal> AssetClassAllocation =>
            HoldingsList.GroupBy(h => h.AssetClass)
                   .ToDictionary(g => g.Key, g => g.Sum(h => h.Weight));
    }
}