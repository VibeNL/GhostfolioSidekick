using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.Globalization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class HoldingDetail
    {
        [Parameter]
        public string Symbol { get; set; } = string.Empty;

        // State
        private bool IsLoading { get; set; } = true;
        private bool HasError { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;
        private string TimeRange { get; set; } = "6M";

        // Data
        private HoldingDisplayModel? HoldingInfo { get; set; }
        private List<HoldingPriceHistoryPoint> PriceHistory { get; set; } = new();

        // Plotly chart
        private Config plotConfig = new();
        private Plotly.Blazor.Layout plotLayout = new();
        private IList<ITrace> plotData = new List<ITrace>();

        protected override async Task OnInitializedAsync()
        {
            await LoadHoldingDataAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(Symbol))
            {
                await LoadHoldingDataAsync();
            }
        }

        private async Task LoadHoldingDataAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = string.Empty;
                StateHasChanged();

                await Task.Yield();

                if (HoldingsDataService == null)
                {
                    throw new InvalidOperationException("HoldingsDataService is not initialized.");
                }

                // Load all holdings to find the specific one
                var allHoldings = await HoldingsDataService.GetHoldingsAsync(Currency.EUR);
                HoldingInfo = allHoldings.FirstOrDefault(h => 
                    string.Equals(h.Symbol, Symbol, StringComparison.OrdinalIgnoreCase));

                if (HoldingInfo != null)
                {
                    // Load price history
                    await LoadPriceHistoryAsync();
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadPriceHistoryAsync()
        {
            try
            {
                if (HoldingsDataService == null || HoldingInfo == null)
                    return;

                var (startDate, endDate) = GetDateRangeFromTimeRange();
                PriceHistory = await HoldingsDataService.GetHoldingPriceHistoryAsync(
                    Symbol, 
                    startDate, 
                    endDate);

                await PrepareChartData();
            }
            catch (Exception ex)
            {
                // Log the error but don't show it as critical - price history might not be available
                Console.WriteLine($"Failed to load price history for {Symbol}: {ex.Message}");
                PriceHistory = new List<HoldingPriceHistoryPoint>();
            }
        }

        private async Task SetTimeRange(string timeRange)
        {
            TimeRange = timeRange;
            await LoadPriceHistoryAsync();
        }

        private async Task RefreshDataAsync()
        {
            await LoadHoldingDataAsync();
        }

        private (DateTime startDate, DateTime endDate) GetDateRangeFromTimeRange()
        {
            var endDate = DateTime.Today;
            var startDate = TimeRange switch
            {
                "1M" => endDate.AddMonths(-1),
                "3M" => endDate.AddMonths(-3),
                "6M" => endDate.AddMonths(-6),
                "1Y" => endDate.AddYears(-1),
                "ALL" => new DateTime(2020, 1, 1), // Default start date for "all" data
                _ => endDate.AddMonths(-6)
            };

            return (startDate, endDate);
        }

        private async Task PrepareChartData()
        {
            if (PriceHistory.Count == 0)
            {
                plotData = new List<ITrace>();
                return;
            }

            await Task.Run(() =>
            {
                var dates = PriceHistory.OrderBy(p => p.Date)
                    .Select(p => p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                    .ToArray();

                // Current market price trace
                var priceTrace = new Scatter
                {
                    X = dates,
                    Y = PriceHistory.OrderBy(p => p.Date)
                        .Select(p => (object)p.Price.Amount)
                        .ToList(),
                    Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
                    Name = $"Market Price",
                    Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#007bff", Width = 2 },
                    Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#007bff", Size = 4 }
                };

                // Average paid price trace
                var averagePriceTrace = new Scatter
                {
                    X = dates,
                    Y = PriceHistory.OrderBy(p => p.Date)
                        .Select(p => (object)p.AveragePrice.Amount)
                        .ToList(),
					Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
					Name = $"Average Paid Price",
                    Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#28a745", Width = 2 },
                    Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#28a745", Size = 4 }
                };

                plotData = new List<ITrace> { priceTrace, averagePriceTrace };

                var currencySymbol = HoldingInfo?.CurrentPrice.Currency.Symbol ?? "USD";
                plotLayout = new Plotly.Blazor.Layout
                {
                    Title = new Plotly.Blazor.LayoutLib.Title { Text = $"{Symbol} Price History ({TimeRange})" },
                    XAxis = new List<Plotly.Blazor.LayoutLib.XAxis> 
                    { 
                        new Plotly.Blazor.LayoutLib.XAxis 
                        { 
                            Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Date" },
                            Type = Plotly.Blazor.LayoutLib.XAxisLib.TypeEnum.Date
                        } 
                    },
                    YAxis = new List<Plotly.Blazor.LayoutLib.YAxis> 
                    { 
                        new Plotly.Blazor.LayoutLib.YAxis 
                        { 
                            Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Price ({currencySymbol})" } 
                        } 
                    },
                    Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 60, R = 30, B = 40 },
                    AutoSize = true,
                    ShowLegend = true,
                    Legend = new List<Plotly.Blazor.LayoutLib.Legend>
                    {
                        new Plotly.Blazor.LayoutLib.Legend
                        {
                            Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.H
                        }
                    }
                };

                plotConfig = new Config { Responsive = true };
            });
        }
    }
}