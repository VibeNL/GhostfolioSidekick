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
        
        // Local fields to track current values and prevent parameter override issues
        private DateTime _currentStartDate;
        private DateTime _currentEndDate;
        private string _currentSelectedCurrency = "EUR";
        private int _currentSelectedAccountId = 0;
        
        // Track when we're in the middle of updating to prevent parameter override
        private bool _isUpdatingCurrency = false;
        private bool _isUpdatingDates = false;
        private bool _isUpdatingAccount = false;

        protected override async Task OnInitializedAsync()
        {
            // Initialize local fields with parameter values
            _currentStartDate = StartDate;
            _currentEndDate = EndDate;
            _currentSelectedCurrency = SelectedCurrency;
            _currentSelectedAccountId = SelectedAccountId;
            
            Console.WriteLine($"OnInitializedAsync - Currency: {_currentSelectedCurrency}");
            
            if (ShowAccountFilter)
            {
                Accounts = await HoldingsDataService.GetAccountsAsync();
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            Console.WriteLine($"OnParametersSetAsync - Parameter: {SelectedCurrency}, Local: {_currentSelectedCurrency}, IsUpdating: {_isUpdatingCurrency}");
            
            // Only update local fields if we're not in the middle of an update and the values have changed
            if (!_isUpdatingDates && (StartDate != _currentStartDate || EndDate != _currentEndDate))
            {
                Console.WriteLine($"Updating dates from parameters - Start: {StartDate}, End: {EndDate}");
                _currentStartDate = StartDate;
                _currentEndDate = EndDate;
            }
            
            if (!_isUpdatingCurrency && SelectedCurrency != _currentSelectedCurrency)
            {
                Console.WriteLine($"Currency parameter changed from {_currentSelectedCurrency} to {SelectedCurrency} - UPDATING LOCAL FIELD");
                _currentSelectedCurrency = SelectedCurrency;
            }
            
            if (!_isUpdatingAccount && SelectedAccountId != _currentSelectedAccountId)
            {
                Console.WriteLine($"AccountId changed from {_currentSelectedAccountId} to {SelectedAccountId}");
                _currentSelectedAccountId = SelectedAccountId;
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
                default:
                    return; // Unknown range, don't change anything
            }

            _isUpdatingDates = true;
            try
            {
                // Update local fields and parameters
                _currentStartDate = newStartDate;
                _currentEndDate = newEndDate;
                StartDate = newStartDate;
                EndDate = newEndDate;
                
                // Notify parent components in the correct sequence
                await StartDateChanged.InvokeAsync(StartDate);
                await EndDateChanged.InvokeAsync(EndDate);
                
                // Give a small delay to ensure the two-way binding has propagated
                await Task.Yield();
                
                // Now notify that all filters have changed
                await OnFiltersChanged.InvokeAsync();
                
                // Force UI update
                StateHasChanged();
            }
            finally
            {
                _isUpdatingDates = false;
            }
        }

        private async Task OnStartDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                _isUpdatingDates = true;
                try
                {
                    _currentStartDate = newDate;
                    StartDate = newDate;
                    _currentDateRange = null; // Clear quick date selection
                    
                    await StartDateChanged.InvokeAsync(StartDate);
                    await Task.Yield(); // Ensure two-way binding completes
                    await OnFiltersChanged.InvokeAsync();
                    StateHasChanged();
                }
                finally
                {
                    _isUpdatingDates = false;
                }
            }
        }

        private async Task OnEndDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                _isUpdatingDates = true;
                try
                {
                    _currentEndDate = newDate;
                    EndDate = newDate;
                    _currentDateRange = null; // Clear quick date selection
                    
                    await EndDateChanged.InvokeAsync(EndDate);
                    await Task.Yield(); // Ensure two-way binding completes
                    await OnFiltersChanged.InvokeAsync();
                    StateHasChanged();
                }
                finally
                {
                    _isUpdatingDates = false;
                }
            }
        }

        private async Task OnCurrencyChangedHandler(ChangeEventArgs e)
        {
            var newCurrency = e.Value?.ToString() ?? "EUR";
            Console.WriteLine($"OnCurrencyChangedHandler - New currency: {newCurrency}, Current local: {_currentSelectedCurrency}, Current parameter: {SelectedCurrency}");
            
            _isUpdatingCurrency = true;
            try
            {
                _currentSelectedCurrency = newCurrency;
                SelectedCurrency = newCurrency;
                
                Console.WriteLine($"Before EventCallbacks - Local: {_currentSelectedCurrency}, Parameter: {SelectedCurrency}");
                
                await SelectedCurrencyChanged.InvokeAsync(SelectedCurrency);
                await Task.Yield(); // Ensure two-way binding completes
                
                Console.WriteLine($"After SelectedCurrencyChanged - Local: {_currentSelectedCurrency}, Parameter: {SelectedCurrency}");
                
                await OnFiltersChanged.InvokeAsync();
                StateHasChanged();
                
                Console.WriteLine($"After OnFiltersChanged - Local: {_currentSelectedCurrency}, Parameter: {SelectedCurrency}");
            }
            finally
            {
                // Add a delay before resetting the flag to prevent immediate parameter override
                await Task.Delay(100);
                _isUpdatingCurrency = false;
                Console.WriteLine($"_isUpdatingCurrency set to false");
            }
        }

        private async Task OnAccountChangedHandler(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var accountId))
            {
                _isUpdatingAccount = true;
                try
                {
                    _currentSelectedAccountId = accountId;
                    SelectedAccountId = accountId;
                    
                    await SelectedAccountIdChanged.InvokeAsync(SelectedAccountId);
                    await Task.Yield(); // Ensure two-way binding completes
                    await OnFiltersChanged.InvokeAsync();
                    StateHasChanged();
                }
                finally
                {
                    _isUpdatingAccount = false;
                }
            }
        }
    }
}