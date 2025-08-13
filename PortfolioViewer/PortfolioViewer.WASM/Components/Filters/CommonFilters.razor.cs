using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Filters
{
    public partial class CommonFilters : ComponentBase
    {
        [Inject] private IHoldingsDataService HoldingsDataService { get; set; } = default!;

        [Parameter] public bool ShowDateFilters { get; set; } = false;
        [Parameter] public bool ShowCurrencyFilter { get; set; } = false;
        [Parameter] public bool ShowAccountFilter { get; set; } = false;

        [Parameter] public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-6);
        [Parameter] public DateTime EndDate { get; set; } = DateTime.Today;
        [Parameter] public string SelectedCurrency { get; set; } = "EUR";
        [Parameter] public int SelectedAccountId { get; set; } = 0;

        [Parameter] public EventCallback<DateTime> StartDateChanged { get; set; }
        [Parameter] public EventCallback<DateTime> EndDateChanged { get; set; }
        [Parameter] public EventCallback<string> SelectedCurrencyChanged { get; set; }
        [Parameter] public EventCallback<int> SelectedAccountIdChanged { get; set; }
        [Parameter] public EventCallback OnFiltersChanged { get; set; }

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

        private async Task SetDateRange(string range)
        {
            _currentDateRange = range;
            var today = DateTime.Today;
            
            DateTime newStartDate = StartDate;
            DateTime newEndDate = EndDate;
            
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
                    newStartDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
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
                    newStartDate = new DateTime(2020, 1, 1); // Adjust to your earliest data date if needed
                    newEndDate = today;
                    break;
            }

            // Update the parameter values
            StartDate = newStartDate;
            EndDate = newEndDate;

            await NotifyFiltersChanged();
        }

        private async Task OnStartDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                StartDate = newDate;
                _currentDateRange = null; // Clear quick date selection
                await NotifyFiltersChanged();
            }
        }

        private async Task OnEndDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                EndDate = newDate;
                _currentDateRange = null; // Clear quick date selection
                await NotifyFiltersChanged();
            }
        }

        private async Task OnCurrencyChangedHandler(ChangeEventArgs e)
        {
            SelectedCurrency = e.Value?.ToString() ?? "EUR";
            await NotifyFiltersChanged();
        }

        private async Task OnAccountChangedHandler(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var accountId))
            {
                SelectedAccountId = accountId;
                await NotifyFiltersChanged();
            }
        }

        private async Task NotifyFiltersChanged()
        {
            // Notify parent components about the changes through EventCallbacks
            await StartDateChanged.InvokeAsync(StartDate);
            await EndDateChanged.InvokeAsync(EndDate);
            await SelectedCurrencyChanged.InvokeAsync(SelectedCurrency);
            await SelectedAccountIdChanged.InvokeAsync(SelectedAccountId);
            await OnFiltersChanged.InvokeAsync();
            
            // Force a re-render to ensure UI updates
            StateHasChanged();
        }
    }
}