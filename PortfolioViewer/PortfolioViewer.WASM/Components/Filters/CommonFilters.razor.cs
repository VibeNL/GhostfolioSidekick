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

        private List<Account> Accounts { get; set; } = new();
        private string? _currentDateRange = null;

        protected override async Task OnInitializedAsync()
        {
            if (ShowAccountFilter)
            {
                Accounts = await HoldingsDataService.GetAccountsAsync();
            }
        }

        private string GetDateRangeButtonClass(string range)
        {
            return _currentDateRange == range ? "btn-primary" : "btn-outline-primary";
        }

        private void SetDateRange(string range)
        {
            if (FilterState == null) return;
            
            _currentDateRange = range;
            var today = DateTime.Today;
            
            DateTime newStartDate;
            DateTime newEndDate;
            
            switch (range)
            {
                case "LastWeek":
                    newStartDate = today.AddDays(-7);
                    newEndDate = today;
                    break;
                case "LastMonth":
                    newStartDate = today.AddMonths(-1);
                    newEndDate = today;
                    break;
                case "YearToDate":
                    newStartDate = new DateTime(today.Year, 1, 1);
                    newEndDate = today;
                    break;
                case "OneYear":
                    newStartDate = today.AddYears(-1);
                    newEndDate = today;
                    break;
                case "FiveYear":
                    newStartDate = today.AddYears(-5);
                    newEndDate = today;
                    break;
                case "Max":
                    newStartDate = new DateTime(2020, 1, 1);
                    newEndDate = today;
                    break;
                default:
                    return;
            }

            // Directly update the FilterState - this will trigger PropertyChanged events
            FilterState.StartDate = newStartDate;
            FilterState.EndDate = newEndDate;
        }

        private void OnStartDateChanged(ChangeEventArgs e)
        {
            if (FilterState == null) return;
            
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                _currentDateRange = null; // Clear quick date selection
                FilterState.StartDate = newDate;
            }
        }

        private void OnEndDateChanged(ChangeEventArgs e)
        {
            if (FilterState == null) return;
            
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                _currentDateRange = null; // Clear quick date selection
                FilterState.EndDate = newDate;
            }
        }

        private void OnCurrencyChanged(ChangeEventArgs e)
        {
            if (FilterState == null) return;
            
            var newCurrency = e.Value?.ToString() ?? "EUR";
            FilterState.SelectedCurrency = newCurrency;
        }

        private void OnAccountChanged(ChangeEventArgs e)
        {
            if (FilterState == null) return;
            
            if (int.TryParse(e.Value?.ToString(), out var accountId))
            {
                FilterState.SelectedAccountId = accountId;
            }
        }
    }
}