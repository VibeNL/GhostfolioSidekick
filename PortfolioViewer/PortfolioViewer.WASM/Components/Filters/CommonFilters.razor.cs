using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Filters
{
    public partial class CommonFilters : ComponentBase
    {
        [Inject] private IHoldingsDataService HoldingsDataService { get; set; } = default!;
        
        [CascadingParameter] private FilterState? FilterState { get; set; }

        [Parameter] public bool ShowDateFilters { get; set; } = false;
        [Parameter] public bool ShowCurrencyFilter { get; set; } = false;
        [Parameter] public bool ShowAccountFilter { get; set; } = false;
        [Parameter] public bool ShowSymbolFilter { get; set; } = false;

        private List<Account> Accounts { get; set; } = new();
        private List<string> Symbols { get; set; } = new();
        private string? _currentDateRange = null;

        protected override async Task OnInitializedAsync()
        {
            if (ShowAccountFilter)
            {
                Accounts = await HoldingsDataService.GetAccountsAsync();
            }
            
            if (ShowSymbolFilter)
            {
                Symbols = await HoldingsDataService.GetSymbolsAsync();
            }
            
            // Detect if FilterState already has YTD dates set and update the button selection
            DetectCurrentDateRange();
        }

        protected override void OnParametersSet()
        {
            // Re-detect the current date range when parameters change
            DetectCurrentDateRange();
        }

        private void DetectCurrentDateRange()
        {
            if (FilterState == null) return;

            var today = DateTime.Today;
            var startOfYear = new DateTime(today.Year, 1, 1);

            // Check if current dates match predefined ranges
            if (FilterState.StartDate.Date == startOfYear && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "YearToDate";
            }
            else if (FilterState.StartDate.Date == today.AddDays(-7) && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "LastWeek";
            }
            else if (FilterState.StartDate.Date == today.AddMonths(-1) && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "LastMonth";
            }
            else if (FilterState.StartDate.Date == today.AddMonths(-3) && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "ThreeMonths";
            }
            else if (FilterState.StartDate.Date == today.AddMonths(-6) && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "SixMonths";
            }
            else if (FilterState.StartDate.Date == today.AddYears(-1) && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "OneYear";
            }
            else if (FilterState.StartDate.Date == today.AddYears(-5) && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "FiveYear";
            }
            else if (FilterState.StartDate.Date == new DateTime(2020, 1, 1) && FilterState.EndDate.Date == today)
            {
                _currentDateRange = "Max";
            }
            else
            {
                _currentDateRange = null; // Custom date range
            }
        }

        private void SetDateRange(string range)
        {
            if (FilterState == null) return;

            var today = DateTime.Today;
            _currentDateRange = range;

            switch (range)
            {
                case "LastWeek":
                    FilterState.StartDate = today.AddDays(-7);
                    FilterState.EndDate = today;
                    break;
                case "LastMonth":
                    FilterState.StartDate = today.AddMonths(-1);
                    FilterState.EndDate = today;
                    break;
                case "ThreeMonths":
                    FilterState.StartDate = today.AddMonths(-3);
                    FilterState.EndDate = today;
                    break;
                case "SixMonths":
                    FilterState.StartDate = today.AddMonths(-6);
                    FilterState.EndDate = today;
                    break;
                case "YearToDate":
                    FilterState.StartDate = new DateTime(today.Year, 1, 1);
                    FilterState.EndDate = today;
                    break;
                case "OneYear":
                    FilterState.StartDate = today.AddYears(-1);
                    FilterState.EndDate = today;
                    break;
                case "FiveYear":
                    FilterState.StartDate = today.AddYears(-5);
                    FilterState.EndDate = today;
                    break;
                case "Max":
                    FilterState.StartDate = new DateTime(2020, 1, 1);
                    FilterState.EndDate = today;
                    break;
            }
        }

        private string GetDateRangeButtonClass(string range)
        {
            return range == _currentDateRange ? "btn-primary" : "btn-outline-primary";
        }

        private void OnStartDateChanged(ChangeEventArgs e)
        {
            if (FilterState != null && DateTime.TryParse(e.Value?.ToString(), out var date))
            {
                FilterState.StartDate = date;
                _currentDateRange = null; // Clear predefined range when custom date is set
            }
        }

        private void OnEndDateChanged(ChangeEventArgs e)
        {
            if (FilterState != null && DateTime.TryParse(e.Value?.ToString(), out var date))
            {
                FilterState.EndDate = date;
                _currentDateRange = null; // Clear predefined range when custom date is set
            }
        }

        private void OnCurrencyChanged(ChangeEventArgs e)
        {
            if (FilterState != null && e.Value != null)
            {
                FilterState.SelectedCurrency = e.Value.ToString() ?? "EUR";
            }
        }

        private void OnAccountChanged(ChangeEventArgs e)
        {
            if (FilterState != null && int.TryParse(e.Value?.ToString(), out var accountId))
            {
                FilterState.SelectedAccountId = accountId;
            }
        }

        private void OnSymbolChanged(ChangeEventArgs e)
        {
            if (FilterState != null && e.Value != null)
            {
                FilterState.SelectedSymbol = e.Value.ToString() ?? "";
            }
        }
    }
}