using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class Holdings
    {
        private string ViewMode = "treemap";
        private List<HoldingDisplayModel> HoldingsList = new();
        private PlotlyChart? treemapChart;
        private Config plotConfig = new();
        private Plotly.Blazor.Layout plotLayout = new();
        private IList<ITrace> plotData = new List<ITrace>();

        protected override async Task OnInitializedAsync()
        {
            await LoadSampleData();
            PrepareTreemapData();
        }

        private void PrepareTreemapData()
        {
            var treemapTrace = new TreeMap
            {
                Labels = HoldingsList.Select(h => h.Symbol).ToArray(),
                Values = HoldingsList.Select(h => (object)h.CurrentValue).ToList(),
                Parents = HoldingsList.Select(h => "").ToArray(),
                Text = HoldingsList.Select(h => $"{h.Symbol}<br>${(h.CurrentValue / 1000):F0}k<br>{h.GainLossPercentage:P1}").ToArray(),
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

        private async Task LoadSampleData()
        {
            // Sample data for demonstration
            HoldingsList = new List<HoldingDisplayModel>
            {
                new HoldingDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Quantity = 100,
                    AveragePrice = 150.00m,
                    CurrentPrice = 175.50m,
                    CurrentValue = 17550.00m,
                    GainLoss = 2550.00m,
                    GainLossPercentage = 0.17m,
                    Weight = 0.35m,
                    Sector = "Technology",
                    AssetClass = "Equity"
                },
                new HoldingDisplayModel
                {
                    Symbol = "GOOGL",
                    Name = "Alphabet Inc.",
                    Quantity = 50,
                    AveragePrice = 2200.00m,
                    CurrentPrice = 2350.00m,
                    CurrentValue = 117500.00m,
                    GainLoss = 7500.00m,
                    GainLossPercentage = 0.068m,
                    Weight = 0.234m,
                    Sector = "Technology",
                    AssetClass = "Equity"
                },
                new HoldingDisplayModel
                {
                    Symbol = "TSLA",
                    Name = "Tesla, Inc.",
                    Quantity = 25,
                    AveragePrice = 800.00m,
                    CurrentPrice = 750.00m,
                    CurrentValue = 18750.00m,
                    GainLoss = -1250.00m,
                    GainLossPercentage = -0.0625m,
                    Weight = 0.0375m,
                    Sector = "Consumer Cyclical",
                    AssetClass = "Equity"
                },
                new HoldingDisplayModel
                {
                    Symbol = "MSFT",
                    Name = "Microsoft Corporation",
                    Quantity = 75,
                    AveragePrice = 280.00m,
                    CurrentPrice = 310.00m,
                    CurrentValue = 23250.00m,
                    GainLoss = 2250.00m,
                    GainLossPercentage = 0.107m,
                    Weight = 0.0465m,
                    Sector = "Technology",
                    AssetClass = "Equity"
                },
                new HoldingDisplayModel
                {
                    Symbol = "NVDA",
                    Name = "NVIDIA Corporation",
                    Quantity = 40,
                    AveragePrice = 220.00m,
                    CurrentPrice = 450.00m,
                    CurrentValue = 18000.00m,
                    GainLoss = 9200.00m,
                    GainLossPercentage = 1.045m,
                    Weight = 0.036m,
                    Sector = "Technology",
                    AssetClass = "Equity"
                },
                new HoldingDisplayModel
                {
                    Symbol = "JPM",
                    Name = "JPMorgan Chase & Co.",
                    Quantity = 60,
                    AveragePrice = 140.00m,
                    CurrentPrice = 155.00m,
                    CurrentValue = 9300.00m,
                    GainLoss = 900.00m,
                    GainLossPercentage = 0.107m,
                    Weight = 0.0186m,
                    Sector = "Financial Services",
                    AssetClass = "Equity"
                },
                new HoldingDisplayModel
                {
                    Symbol = "JNJ",
                    Name = "Johnson & Johnson",
                    Quantity = 80,
                    AveragePrice = 160.00m,
                    CurrentPrice = 165.00m,
                    CurrentValue = 13200.00m,
                    GainLoss = 400.00m,
                    GainLossPercentage = 0.031m,
                    Weight = 0.0264m,
                    Sector = "Healthcare",
                    AssetClass = "Equity"
                },
                new HoldingDisplayModel
                {
                    Symbol = "BTC",
                    Name = "Bitcoin",
                    Quantity = 0.5m,
                    AveragePrice = 45000.00m,
                    CurrentPrice = 42000.00m,
                    CurrentValue = 21000.00m,
                    GainLoss = -1500.00m,
                    GainLossPercentage = -0.067m,
                    Weight = 0.042m,
                    Sector = "Cryptocurrency",
                    AssetClass = "Cryptocurrency"
                }
            };

            // Calculate weights based on total portfolio value
            var totalValue = HoldingsList.Sum(h => h.CurrentValue);
            foreach (var holding in HoldingsList)
            {
                holding.Weight = holding.CurrentValue / totalValue;
            }
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