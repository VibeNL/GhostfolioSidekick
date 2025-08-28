using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Filters
{
    public partial class CommonFilters : ComponentBase
    {
        [Inject] private IHoldingsDataService HoldingsDataService { get; set; } = default!;
        [Inject] private ILogger<CommonFilters>? Logger { get; set; }
        
        [CascadingParameter] private FilterState? FilterState { get; set; }

        [Parameter] public bool ShowDateFilters { get; set; } = false;
        [Parameter] public bool ShowCurrencyFilter { get; set; } = false;
        [Parameter] public bool ShowAccountFilter { get; set; } = false;
        [Parameter] public bool ShowSymbolFilter { get; set; } = false;

        private List<Account> Accounts { get; set; } = new();
        private List<string> Symbols { get; set; } = new();
        private string? _currentDateRange = null;
        
        // Track loading states
        private bool _isLoadingAccounts = false;
        private bool _isLoadingSymbols = false;
        private bool _accountsLoadFailed = false;
        private bool _symbolsLoadFailed = false;

        protected override async Task OnInitializedAsync()
        {
            // Load filter data
            await LoadFilterDataAsync();
            
            // Detect if FilterState already has YTD dates set and update the button selection
            DetectCurrentDateRange();
        }

        protected override async Task OnParametersSetAsync()
        {
            // Re-detect the current date range when parameters change
            DetectCurrentDateRange();
            
            // Reload filter data if the filter requirements have changed
            await LoadFilterDataAsync();
        }

        private async Task LoadFilterDataAsync()
        {
            var tasks = new List<Task>();
            
            if (ShowAccountFilter && Accounts.Count == 0 && !_isLoadingAccounts && !_accountsLoadFailed)
            {
                tasks.Add(LoadAccountsAsync());
            }
            
            if (ShowSymbolFilter && Symbols.Count == 0 && !_isLoadingSymbols && !_symbolsLoadFailed)
            {
                tasks.Add(LoadSymbolsAsync());
            }
            
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
                StateHasChanged();
            }
        }

        private async Task LoadAccountsAsync()
        {
            if (_isLoadingAccounts) return; // Prevent concurrent loads
            
            try
            {
                _isLoadingAccounts = true;
                _accountsLoadFailed = false;
                Logger?.LogInformation("Loading accounts for filter...");
                
                // Add timeout to prevent infinite loading
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var accounts = await HoldingsDataService.GetAccountsAsync().WaitAsync(cts.Token);
                
                // Only update if we got a valid result
                if (accounts != null)
                {
                    Accounts = accounts;
                    Logger?.LogInformation("Successfully loaded {Count} accounts for filter", Accounts.Count);
                }
                else
                {
                    Logger?.LogWarning("GetAccountsAsync returned null");
                    Accounts = new List<Account>();
                }
            }
            catch (OperationCanceledException)
            {
                _accountsLoadFailed = true;
                Logger?.LogError("Timeout loading accounts for filter");
                Accounts = new List<Account>();
                
                // Ensure filter state remains consistent
                if (FilterState != null && FilterState.SelectedAccountId > 0)
                {
                    Logger?.LogWarning("Resetting selected account due to timeout");
                    FilterState.SelectedAccountId = 0;
                }
            }
            catch (Exception ex)
            {
                _accountsLoadFailed = true;
                Logger?.LogError(ex, "Failed to load accounts for filter");
                Accounts = new List<Account>();
                
                // Ensure filter state remains consistent
                if (FilterState != null && FilterState.SelectedAccountId > 0)
                {
                    Logger?.LogWarning("Resetting selected account due to load failure");
                    FilterState.SelectedAccountId = 0;
                }
            }
            finally
            {
                _isLoadingAccounts = false;
            }
        }

        private async Task LoadSymbolsAsync()
        {
            if (_isLoadingSymbols) return; // Prevent concurrent loads
            
            try
            {
                _isLoadingSymbols = true;
                _symbolsLoadFailed = false;
                Logger?.LogInformation("Loading symbols for filter...");
                
                // Add timeout to prevent infinite loading
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var symbols = await HoldingsDataService.GetSymbolsAsync().WaitAsync(cts.Token);
                
                // Only update if we got a valid result
                if (symbols != null)
                {
                    Symbols = symbols;
                    Logger?.LogInformation("Successfully loaded {Count} symbols for filter", Symbols.Count);
                }
                else
                {
                    Logger?.LogWarning("GetSymbolsAsync returned null");
                    Symbols = new List<string>();
                }
            }
            catch (OperationCanceledException)
            {
                _symbolsLoadFailed = true;
                Logger?.LogError("Timeout loading symbols for filter");
                Symbols = new List<string>();
                
                // Ensure filter state remains consistent
                if (FilterState != null && !string.IsNullOrEmpty(FilterState.SelectedSymbol))
                {
                    Logger?.LogWarning("Resetting selected symbol due to timeout");
                    FilterState.SelectedSymbol = "";
                }
            }
            catch (Exception ex)
            {
                _symbolsLoadFailed = true;
                Logger?.LogError(ex, "Failed to load symbols for filter");
                Symbols = new List<string>();
                
                // Ensure filter state remains consistent
                if (FilterState != null && !string.IsNullOrEmpty(FilterState.SelectedSymbol))
                {
                    Logger?.LogWarning("Resetting selected symbol due to load failure");
                    FilterState.SelectedSymbol = "";
                }
            }
            finally
            {
                _isLoadingSymbols = false;
            }
        }

        private async Task RetryLoadAccountsAsync()
        {
            Logger?.LogInformation("Retrying to load accounts...");
            _accountsLoadFailed = false;
            Accounts.Clear();
            await LoadAccountsAsync();
            StateHasChanged();
        }

        private async Task RetryLoadSymbolsAsync()
        {
            Logger?.LogInformation("Retrying to load symbols...");
            _symbolsLoadFailed = false;
            Symbols.Clear();
            await LoadSymbolsAsync();
            StateHasChanged();
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