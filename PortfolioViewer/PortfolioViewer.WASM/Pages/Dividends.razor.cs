using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using GhostfolioSidekick.Model.Accounts;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.Globalization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class Dividends
    {
        [Inject]
        private IDividendsDataService? DividendsDataService { get; set; }

        // View settings
        private string ViewMode = "chart";
        private string ChartType = "monthly"; // monthly, yearly, individual
        private string SelectedCurrency = "EUR";
        private DateTime StartDate = DateTime.Today.AddYears(-2);
        private DateTime EndDate = DateTime.Today;
        private DateOnly MinDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-10));

        // Filters
        private int SelectedAccountId = 0;
        private string SelectedSymbol = "";
        private string SelectedAssetClass = "";

        // Data
        private List<DividendAggregateDisplayModel> AggregatedDividends = new();
        private List<DividendDisplayModel> IndividualDividends = new();
        private List<Account> Accounts = new();
        private List<string> AvailableSymbols = new();
        private List<string> AvailableAssetClasses = new();

        // Chart data
        private Config plotConfig = new();
        private Plotly.Blazor.Layout plotLayout = new();
        private IList<ITrace> plotData = new List<ITrace>();

        // State management
        private bool IsLoading { get; set; } = true;
        private bool HasError { get; set; } = false;
        private string ErrorMessage { get; set; } = string.Empty;

        // Sorting
        private string sortColumn = "Date";
        private bool sortAscending = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (DividendsDataService != null)
                {
                    MinDate = await DividendsDataService.GetMinDividendDateAsync();
                    Accounts = await DividendsDataService.GetAccountsAsync();
                    AvailableSymbols = await DividendsDataService.GetDividendSymbolsAsync();
                    AvailableAssetClasses = await DividendsDataService.GetDividendAssetClassesAsync();
                    
                    StartDate = MinDate.ToDateTime(TimeOnly.MinValue);
                }
                
                await LoadDividendsAsync();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadDividendsAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = string.Empty;
                StateHasChanged();
                await Task.Yield();

                if (DividendsDataService == null)
                {
                    throw new InvalidOperationException("DividendsDataService is not initialized.");
                }

                var currency = Currency.GetCurrency(SelectedCurrency);

                if (ViewMode == "table")
                {
                    if (ChartType == "individual")
                    {
                        IndividualDividends = await DividendsDataService.GetDividendsAsync(
                            currency, StartDate, EndDate, SelectedAccountId, SelectedSymbol, SelectedAssetClass);
                        SortIndividualDividends();
                    }
                    else if (ChartType == "monthly")
                    {
                        AggregatedDividends = await DividendsDataService.GetMonthlyDividendsAsync(
                            currency, StartDate, EndDate, SelectedAccountId, SelectedSymbol, SelectedAssetClass);
                    }
                    else if (ChartType == "yearly")
                    {
                        AggregatedDividends = await DividendsDataService.GetYearlyDividendsAsync(
                            currency, StartDate, EndDate, SelectedAccountId, SelectedSymbol, SelectedAssetClass);
                    }
                }
                else // chart mode
                {
                    // Load both aggregated and individual data for chart
                    if (ChartType == "monthly")
                    {
                        AggregatedDividends = await DividendsDataService.GetMonthlyDividendsAsync(
                            currency, StartDate, EndDate, SelectedAccountId, SelectedSymbol, SelectedAssetClass);
                    }
                    else if (ChartType == "yearly")
                    {
                        AggregatedDividends = await DividendsDataService.GetYearlyDividendsAsync(
                            currency, StartDate, EndDate, SelectedAccountId, SelectedSymbol, SelectedAssetClass);
                    }
                    else // individual
                    {
                        IndividualDividends = await DividendsDataService.GetDividendsAsync(
                            currency, StartDate, EndDate, SelectedAccountId, SelectedSymbol, SelectedAssetClass);
                        
                        // Group individual dividends by month for chart display
                        AggregatedDividends = IndividualDividends
                            .GroupBy(d => new { Year = d.Date.Year, Month = d.Date.Month })
                            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                            .Select(g => new DividendAggregateDisplayModel
                            {
                                Period = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                                TotalAmount = Money.Sum(g.Select(d => d.Amount)),
                                TotalTaxAmount = Money.Sum(g.Select(d => d.TaxAmount)),
                                TotalNetAmount = Money.Sum(g.Select(d => d.NetAmount)),
                                DividendCount = g.Count(),
                                Dividends = g.ToList()
                            })
                            .ToList();
                    }
                    
                    await PrepareChartData();
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

        private async Task PrepareChartData()
        {
            if (AggregatedDividends.Count == 0)
            {
                plotData = new List<ITrace>();
                return;
            }

            // Prepare data for bar chart
            var periods = AggregatedDividends.Select(d => d.Period).ToArray();
            var grossAmounts = AggregatedDividends.Select(d => (object)d.TotalAmount.Amount).ToList();
            var taxAmounts = AggregatedDividends.Select(d => (object)d.TotalTaxAmount.Amount).ToList();
            var netAmounts = AggregatedDividends.Select(d => (object)d.TotalNetAmount.Amount).ToList();

            // Create bar traces
            var grossTrace = new Bar
            {
                X = periods,
                Y = grossAmounts,
                Name = "Gross Dividends",
                Marker = new Plotly.Blazor.Traces.BarLib.Marker { Color = "#28a745" }
            };

            var taxTrace = new Bar
            {
                X = periods,
                Y = taxAmounts,
                Name = "Tax Amount",
                Marker = new Plotly.Blazor.Traces.BarLib.Marker { Color = "#dc3545" }
            };

            var netTrace = new Bar
            {
                X = periods,
                Y = netAmounts,
                Name = "Net Dividends",
                Marker = new Plotly.Blazor.Traces.BarLib.Marker { Color = "#007bff" }
            };

            plotData = new List<ITrace> { grossTrace, taxTrace, netTrace };

            // Configure layout
            var periodLabel = ChartType == "yearly" ? "Year" : "Period";
            plotLayout = new Plotly.Blazor.Layout
            {
                Title = new Plotly.Blazor.LayoutLib.Title 
                { 
                    Text = $"Dividend Income Over Time ({ChartType.Substring(0, 1).ToUpper() + ChartType.Substring(1)})" 
                },
                XAxis = new List<Plotly.Blazor.LayoutLib.XAxis> 
                { 
                    new Plotly.Blazor.LayoutLib.XAxis 
                    { 
                        Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = periodLabel },
                        TickAngle = -45
                    } 
                },
                YAxis = new List<Plotly.Blazor.LayoutLib.YAxis> 
                { 
                    new Plotly.Blazor.LayoutLib.YAxis 
                    { 
                        Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Amount ({SelectedCurrency})" } 
                    } 
                },
                Margin = new Plotly.Blazor.LayoutLib.Margin { T = 60, L = 60, R = 30, B = 100 },
                AutoSize = true,
                ShowLegend = true,
                Legend = new List<Plotly.Blazor.LayoutLib.Legend>
                {
                    new Plotly.Blazor.LayoutLib.Legend
                    {
                        Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.H,
                        Y = -0.3m
                    }
                },
                BarMode = Plotly.Blazor.LayoutLib.BarModeEnum.Group
            };

            plotConfig = new Config { Responsive = true };

            await Task.Delay(1); // Ensure UI updates
        }

        private async Task RefreshDataAsync()
        {
            await LoadDividendsAsync();
        }

        private async Task OnFiltersChanged()
        {
            await LoadDividendsAsync();
        }

        private async Task OnViewModeChanged(string newViewMode)
        {
            ViewMode = newViewMode;
            await LoadDividendsAsync();
        }

        private async Task OnChartTypeChanged(string newChartType)
        {
            ChartType = newChartType;
            await LoadDividendsAsync();
        }

        private void SetDateRange(string range)
        {
            var today = DateTime.Today;
            switch (range)
            {
                case "LastMonth":
                    StartDate = today.AddMonths(-1);
                    EndDate = today;
                    break;
                case "LastQuarter":
                    StartDate = today.AddMonths(-3);
                    EndDate = today;
                    break;
                case "YearToDate":
                    StartDate = new DateTime(today.Year, 1, 1);
                    EndDate = today;
                    break;
                case "LastYear":
                    StartDate = today.AddYears(-1);
                    EndDate = today;
                    break;
                case "FiveYear":
                    StartDate = today.AddYears(-5);
                    EndDate = today;
                    break;
                case "Max":
                    StartDate = MinDate.ToDateTime(TimeOnly.MinValue);
                    EndDate = today;
                    break;
            }

            _ = LoadDividendsAsync();
        }

        private void SortBy(string column)
        {
            if (sortColumn == column)
            {
                sortAscending = !sortAscending;
            }
            else
            {
                sortColumn = column;
                sortAscending = true;
            }

            if (ChartType == "individual")
            {
                SortIndividualDividends();
            }
            else
            {
                SortAggregatedDividends();
            }
        }

        private void SortIndividualDividends()
        {
            switch (sortColumn)
            {
                case "Date":
                    IndividualDividends = sortAscending ? IndividualDividends.OrderBy(d => d.Date).ToList() 
                                                        : IndividualDividends.OrderByDescending(d => d.Date).ToList();
                    break;
                case "Symbol":
                    IndividualDividends = sortAscending ? IndividualDividends.OrderBy(d => d.Symbol).ToList() 
                                                        : IndividualDividends.OrderByDescending(d => d.Symbol).ToList();
                    break;
                case "Name":
                    IndividualDividends = sortAscending ? IndividualDividends.OrderBy(d => d.Name).ToList() 
                                                        : IndividualDividends.OrderByDescending(d => d.Name).ToList();
                    break;
                case "Amount":
                    IndividualDividends = sortAscending ? IndividualDividends.OrderBy(d => d.Amount.Amount).ToList() 
                                                        : IndividualDividends.OrderByDescending(d => d.Amount.Amount).ToList();
                    break;
                case "TaxAmount":
                    IndividualDividends = sortAscending ? IndividualDividends.OrderBy(d => d.TaxAmount.Amount).ToList() 
                                                        : IndividualDividends.OrderByDescending(d => d.TaxAmount.Amount).ToList();
                    break;
                case "NetAmount":
                    IndividualDividends = sortAscending ? IndividualDividends.OrderBy(d => d.NetAmount.Amount).ToList() 
                                                        : IndividualDividends.OrderByDescending(d => d.NetAmount.Amount).ToList();
                    break;
                case "Account":
                    IndividualDividends = sortAscending ? IndividualDividends.OrderBy(d => d.AccountName).ToList() 
                                                        : IndividualDividends.OrderByDescending(d => d.AccountName).ToList();
                    break;
                default:
                    break;
            }
        }

        private void SortAggregatedDividends()
        {
            switch (sortColumn)
            {
                case "Period":
                    AggregatedDividends = sortAscending ? AggregatedDividends.OrderBy(d => d.Date).ToList() 
                                                        : AggregatedDividends.OrderByDescending(d => d.Date).ToList();
                    break;
                case "TotalAmount":
                    AggregatedDividends = sortAscending ? AggregatedDividends.OrderBy(d => d.TotalAmount.Amount).ToList() 
                                                        : AggregatedDividends.OrderByDescending(d => d.TotalAmount.Amount).ToList();
                    break;
                case "TotalTaxAmount":
                    AggregatedDividends = sortAscending ? AggregatedDividends.OrderBy(d => d.TotalTaxAmount.Amount).ToList() 
                                                        : AggregatedDividends.OrderByDescending(d => d.TotalTaxAmount.Amount).ToList();
                    break;
                case "TotalNetAmount":
                    AggregatedDividends = sortAscending ? AggregatedDividends.OrderBy(d => d.TotalNetAmount.Amount).ToList() 
                                                        : AggregatedDividends.OrderByDescending(d => d.TotalNetAmount.Amount).ToList();
                    break;
                case "DividendCount":
                    AggregatedDividends = sortAscending ? AggregatedDividends.OrderBy(d => d.DividendCount).ToList() 
                                                        : AggregatedDividends.OrderByDescending(d => d.DividendCount).ToList();
                    break;
                default:
                    break;
            }
        }

        // Computed properties for summary statistics
        private Money TotalDividends => ViewMode == "table" && ChartType == "individual" 
            ? Money.Sum(IndividualDividends.Select(d => d.Amount))
            : Money.Sum(AggregatedDividends.Select(d => d.TotalAmount));

        private Money TotalTaxes => ViewMode == "table" && ChartType == "individual" 
            ? Money.Sum(IndividualDividends.Select(d => d.TaxAmount))
            : Money.Sum(AggregatedDividends.Select(d => d.TotalTaxAmount));

        private Money TotalNet => ViewMode == "table" && ChartType == "individual" 
            ? Money.Sum(IndividualDividends.Select(d => d.NetAmount))
            : Money.Sum(AggregatedDividends.Select(d => d.TotalNetAmount));

        private int TotalDividendCount => ViewMode == "table" && ChartType == "individual" 
            ? IndividualDividends.Count
            : AggregatedDividends.Sum(d => d.DividendCount);
    }
}