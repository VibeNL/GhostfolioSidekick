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
        private bool _isUpdating = false;

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

        protected override void OnParametersSet()
        {
            Console.WriteLine($"OnParametersSetAsync - Parameter: {SelectedCurrency}, Local: {_currentSelectedCurrency}, IsUpdating: {_isUpdating}");
            
            // Only update local fields if we're not in the middle of an update to prevent circular updates
            if (!_isUpdating)
            {
                bool hasChanges = false;
                
                if (StartDate != _currentStartDate)
                {
                    Console.WriteLine($"Updating start date from parameters - New: {StartDate}");
                    _currentStartDate = StartDate;
                    hasChanges = true;
                }
                
                if (EndDate != _currentEndDate)
                {
                    Console.WriteLine($"Updating end date from parameters - New: {EndDate}");
                    _currentEndDate = EndDate;
                    hasChanges = true;
                }
                
                if (SelectedCurrency != _currentSelectedCurrency)
                {
                    Console.WriteLine($"Currency parameter changed from {_currentSelectedCurrency} to {SelectedCurrency} - UPDATING LOCAL FIELD");
                    _currentSelectedCurrency = SelectedCurrency;
                    hasChanges = true;
                }
                
                if (SelectedAccountId != _currentSelectedAccountId)
                {
                    Console.WriteLine($"AccountId changed from {_currentSelectedAccountId} to {SelectedAccountId}");
                    _currentSelectedAccountId = SelectedAccountId;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    // Clear date range selection if parameters changed externally
                    _currentDateRange = null;
                }
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

            await UpdateDateParameters(newStartDate, newEndDate);
        }

        private async Task OnStartDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                _currentDateRange = null; // Clear quick date selection
                await UpdateDateParameters(newDate, _currentEndDate);
            }
        }

        private async Task OnEndDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out var newDate))
            {
                _currentDateRange = null; // Clear quick date selection
                await UpdateDateParameters(_currentStartDate, newDate);
            }
        }

        private async Task UpdateDateParameters(DateTime newStartDate, DateTime newEndDate)
        {
            _isUpdating = true;
            try
            {
                // Update local fields first
                _currentStartDate = newStartDate;
                _currentEndDate = newEndDate;
                
                // Notify parent components
                await StartDateChanged.InvokeAsync(newStartDate);
                await EndDateChanged.InvokeAsync(newEndDate);
                await OnFiltersChanged.InvokeAsync();
                
                StateHasChanged();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private async Task OnCurrencyChangedHandler(ChangeEventArgs e)
        {
            var newCurrency = e.Value?.ToString() ?? "EUR";
            Console.WriteLine($"OnCurrencyChangedHandler - New currency: {newCurrency}, Current local: {_currentSelectedCurrency}");
            
            _isUpdating = true;
            try
            {
                _currentSelectedCurrency = newCurrency;
                
                Console.WriteLine($"Before EventCallbacks - Local: {_currentSelectedCurrency}");
                
                await SelectedCurrencyChanged.InvokeAsync(newCurrency);
                await OnFiltersChanged.InvokeAsync();
                
                StateHasChanged();
                
                Console.WriteLine($"After EventCallbacks - Local: {_currentSelectedCurrency}");
            }
            finally
            {
                _isUpdating = false;
                Console.WriteLine($"_isUpdating set to false");
            }
        }

        private async Task OnAccountChangedHandler(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var accountId))
            {
                _isUpdating = true;
                try
                {
                    _currentSelectedAccountId = accountId;
                    
                    await SelectedAccountIdChanged.InvokeAsync(accountId);
                    await OnFiltersChanged.InvokeAsync();
                    StateHasChanged();
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }
    }
}