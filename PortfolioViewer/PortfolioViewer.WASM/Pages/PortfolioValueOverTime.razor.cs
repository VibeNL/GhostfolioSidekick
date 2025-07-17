using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using GhostfolioSidekick.PortfolioViewer.Services.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class PortfolioValueOverTime
    {
        [Inject]
        private IPortfolioValueService PortfolioValueService { get; set; } = default!;

        private bool isLoading = true;
        private string selectedTimeframe = "1y";
        private string selectedCurrency = "USD";

        private List<string>? AvailableCurrencies;
        private List<PortfolioValuePoint>? PortfolioData;
        private List<AccountBreakdown>? PortfolioBreakdown;
        private PortfolioSummary? Summary;

        // Chart data for Radzen
        private List<ChartDataItem> PortfolioChartData = new();
        private List<ChartDataItem> InvestedChartData = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
            isLoading = false;
        }

        private async Task LoadData()
        {
            try
            {
                AvailableCurrencies = await PortfolioValueService.GetAvailableCurrenciesAsync();
                selectedCurrency = AvailableCurrencies.FirstOrDefault() ?? "USD";
                await LoadPortfolioData();
            }
            catch (Exception ex)
            {
                // Log error in production
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private async Task LoadPortfolioData()
        {
            try
            {
                // Load portfolio data
                PortfolioData = await PortfolioValueService.GetPortfolioValueOverTimeAsync(selectedTimeframe, selectedCurrency);

                // Prepare chart data
                PrepareChartData();

                // Calculate summary statistics
                if (PortfolioData != null)
                {
                    Summary = await PortfolioValueService.GetPortfolioSummaryAsync(PortfolioData, selectedCurrency);
                }

                // Load portfolio breakdown by account
                PortfolioBreakdown = await PortfolioValueService.GetPortfolioBreakdownAsync(selectedCurrency);
            }
            catch (Exception ex)
            {
                // Log error in production
                Console.WriteLine($"Error loading portfolio data: {ex.Message}");
            }
        }

        private void PrepareChartData()
        {
            if (PortfolioData == null || !PortfolioData.Any())
            {
                PortfolioChartData = new();
                InvestedChartData = new();
                return;
            }

            var sortedData = PortfolioData.OrderBy(p => p.Date).ToList();

            PortfolioChartData = sortedData.Select(p => new ChartDataItem 
            { 
                Date = p.Date, 
                Value = (double)p.TotalValue 
            }).ToList();

            InvestedChartData = sortedData.Select(p => new ChartDataItem 
            { 
                Date = p.Date, 
                Value = (double)p.CumulativeInvested 
            }).ToList();
        }

        private async Task RefreshChart()
        {
            isLoading = true;
            StateHasChanged();

            await LoadPortfolioData();

            isLoading = false;
            StateHasChanged();
        }

        // Chart formatting methods
        private string FormatAsValue(object value)
        {
            if (double.TryParse(value?.ToString(), out double d))
            {
                return d.ToString("C0");
            }
            return value?.ToString() ?? "";
        }

        private string FormatAsDate(object value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("MMM dd");
            }
            return value?.ToString() ?? "";
        }

        // Properties for UI binding
        public string CurrentPortfolioValue => Summary?.CurrentPortfolioValue ?? "N/A";
        public string CurrentValueDate => Summary?.CurrentValueDate ?? "N/A";
        public decimal TotalReturnAmount => Summary?.TotalReturnAmount ?? 0;
        public decimal TotalReturnPercent => Summary?.TotalReturnPercent ?? 0;
        public string TotalInvestedAmount => Summary?.TotalInvestedAmount ?? "N/A";
    }
}