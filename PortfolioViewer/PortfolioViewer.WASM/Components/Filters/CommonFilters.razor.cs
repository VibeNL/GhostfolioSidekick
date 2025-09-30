using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components.Filters
{
	public partial class CommonFilters : ComponentBase, IDisposable
	{
		[Inject] private IAccountDataService AccountDataService { get; set; } = default!;
		[Inject] private ILogger<CommonFilters>? Logger { get; set; }

		[CascadingParameter] private FilterState? FilterState { get; set; }

		[Parameter] public bool ShowDateFilters { get; set; } = false;
		[Parameter] public bool ShowAccountFilter { get; set; } = false;
		[Parameter] public bool ShowSymbolFilter { get; set; } = false;
		[Parameter] public bool ShowTransactionTypeFilter { get; set; } = false;
		[Parameter] public bool ShowSearchFilter { get; set; } = false;
		[Parameter] public bool ShowApplyButton { get; set; } = true;
		[Parameter] public EventCallback OnFiltersApplied { get; set; }
		[Parameter] public List<string>? TransactionTypes { get; set; } = null;

		// Pending filter state that holds changes before applying
		private PendingFilterState _pendingFilterState = new();

		private List<Account> Accounts { get; set; } = new();
		private List<string> Symbols { get; set; } = new();

		// Store all available accounts and symbols for filtering
		private List<Account> _allAccounts = new();
		private List<string> _allSymbols = new();

		private string? _currentDateRange = null;

		// Track loading states
		private bool _isLoadingAccounts = false;
		private bool _isLoadingSymbols = false;

		// Search debouncing
		private Timer? _searchDebounceTimer;

		// Track previous parameter state to detect changes
		private bool _previousShowAccountFilter = false;
		private bool _previousShowSymbolFilter = false;
		private bool _isFirstLoad = true;

		protected override async Task OnInitializedAsync()
		{
			// Initialize pending state from current filter state
			if (FilterState != null)
			{
				_pendingFilterState = PendingFilterState.FromFilterState(FilterState);
			}

			// Store initial parameter state
			_previousShowAccountFilter = ShowAccountFilter;
			_previousShowSymbolFilter = ShowSymbolFilter;
			_isFirstLoad = true;

			// Load filter data
			await LoadFilterDataAsync();

			// Detect if FilterState already has YTD dates set and update the button selection
			DetectCurrentDateRange();
		}

		protected override async Task OnParametersSetAsync()
		{
			// Update pending state if FilterState changed externally
			if (FilterState != null && !_pendingFilterState.HasChanges(FilterState))
			{
				_pendingFilterState = PendingFilterState.FromFilterState(FilterState);
			}

			// Check if filter visibility has changed (indicating page switch)
			bool filterVisibilityChanged = false;
			if (!_isFirstLoad)
			{
				if (_previousShowAccountFilter != ShowAccountFilter)
				{
					filterVisibilityChanged = true;
					Logger?.LogInformation("Account filter visibility changed from {Previous} to {Current}", _previousShowAccountFilter, ShowAccountFilter);
				}

				if (_previousShowSymbolFilter != ShowSymbolFilter)
				{
					filterVisibilityChanged = true;
					Logger?.LogInformation("Symbol filter visibility changed from {Previous} to {Current}", _previousShowSymbolFilter, ShowSymbolFilter);
				}
			}

			// Reset inactive filters if visibility changed (page switch detected)
			if (filterVisibilityChanged && FilterState != null)
			{
				await ResetInactiveFilters();
			}

			// Update previous state for next comparison
			_previousShowAccountFilter = ShowAccountFilter;
			_previousShowSymbolFilter = ShowSymbolFilter;
			_isFirstLoad = false;

			// Re-detect the current date range when parameters change
			DetectCurrentDateRange();

			// Reload filter data if the filter requirements have changed
			await LoadFilterDataAsync();
		}

		private async Task ResetInactiveFilters()
		{
			if (FilterState == null) return;

			bool hasChanges = false;

			// Reset account filter if it's not shown on current page
			if (!ShowAccountFilter && FilterState.SelectedAccountId != 0)
			{
				FilterState.SelectedAccountId = 0;
				_pendingFilterState.SelectedAccountId = 0;
				hasChanges = true;
				Logger?.LogInformation("Reset account filter to 'All Accounts' (not shown on current page)");
			}

			// Reset symbol filter if it's not shown on current page
			if (!ShowSymbolFilter && !string.IsNullOrEmpty(FilterState.SelectedSymbol))
			{
				FilterState.SelectedSymbol = "";
				_pendingFilterState.SelectedSymbol = "";
				hasChanges = true;
				Logger?.LogInformation("Reset symbol filter to 'All Symbols' (not shown on current page)");
			}

			// Reset transaction type filter if it's not shown on current page
			if (!ShowTransactionTypeFilter && !string.IsNullOrEmpty(FilterState.SelectedTransactionType))
			{
				FilterState.SelectedTransactionType = "";
				_pendingFilterState.SelectedTransactionType = "";
				hasChanges = true;
				Logger?.LogInformation("Reset transaction type filter to 'All Types' (not shown on current page)");
			}

			// Reset search filter if it's not shown on current page
			if (!ShowSearchFilter && !string.IsNullOrEmpty(FilterState.SearchText))
			{
				FilterState.SearchText = "";
				_pendingFilterState.SearchText = "";
				hasChanges = true;
				Logger?.LogInformation("Reset search filter (not shown on current page)");
			}

			if (hasChanges)
			{
				// Update filtered options after resetting
				await UpdateFilteredOptionsAsync();
				StateHasChanged();
			}
		}

		private async Task LoadFilterDataAsync()
		{
			var tasks = new List<Task>();

			if (ShowAccountFilter && _allAccounts.Count == 0 && !_isLoadingAccounts)
			{
				tasks.Add(LoadAccountsAsync());
			}

			if (ShowSymbolFilter && _allSymbols.Count == 0 && !_isLoadingSymbols)
			{
				tasks.Add(LoadSymbolsAsync());
			}

			if (tasks.Count > 0)
			{
				await Task.WhenAll(tasks);
				await UpdateFilteredOptionsAsync();
				StateHasChanged();
			}
			else if (_allAccounts.Count > 0 || _allSymbols.Count > 0)
			{
				// Update filtered options if we already have data
				await UpdateFilteredOptionsAsync();
			}
		}

		private async Task UpdateFilteredOptionsAsync()
		{
			try
			{
				// Update accounts based on selected symbol in pending state
				if (ShowAccountFilter && !string.IsNullOrEmpty(_pendingFilterState.SelectedSymbol))
				{
					Accounts = await AccountDataService.GetAccountsAsync(_pendingFilterState.SelectedSymbol);

					// Check if currently selected account is still valid
					if (_pendingFilterState.SelectedAccountId > 0 && !Accounts.Any(a => a.Id == _pendingFilterState.SelectedAccountId))
					{
						_pendingFilterState.SelectedAccountId = 0; // Reset to "All Accounts"
					}
				}
				else if (ShowAccountFilter)
				{
					Accounts = _allAccounts;
				}

				// Update symbols based on selected account in pending state
				if (ShowSymbolFilter && _pendingFilterState.SelectedAccountId > 0)
				{
					Symbols = await AccountDataService.GetSymbolProfilesAsync(_pendingFilterState.SelectedAccountId);

					// Check if currently selected symbol is still valid
					if (!string.IsNullOrEmpty(_pendingFilterState.SelectedSymbol) && !Symbols.Contains(_pendingFilterState.SelectedSymbol))
					{
						_pendingFilterState.SelectedSymbol = ""; // Reset to "All Symbols"
					}
				}
				else if (ShowSymbolFilter)
				{
					Symbols = _allSymbols;
				}
			}
			catch (Exception ex)
			{
				Logger?.LogError(ex, "Failed to update filtered options");
			}
		}

		private async Task LoadAccountsAsync()
		{
			if (_isLoadingAccounts) return; // Prevent concurrent loads

			try
			{
				_isLoadingAccounts = true;
				Logger?.LogInformation("Loading accounts for filter...");

				// Add timeout to prevent infinite loading
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
				var accounts = await AccountDataService.GetAccountsAsync(null, cts.Token);

				// Only update if we got a valid result
				if (accounts != null)
				{
					_allAccounts = accounts;
					Accounts = _allAccounts; // Initially show all accounts
					Logger?.LogInformation("Successfully loaded {Count} accounts for filter", _allAccounts.Count);
				}
				else
				{
					Logger?.LogWarning("GetAccountsAsync returned null");
					_allAccounts = new List<Account>();
					Accounts = new List<Account>();
				}
			}
			catch (OperationCanceledException)
			{
				Logger?.LogError("Timeout loading accounts for filter");
				_allAccounts = new List<Account>();
				Accounts = new List<Account>();

				// Ensure pending state remains consistent
				if (_pendingFilterState.SelectedAccountId > 0)
				{
					Logger?.LogWarning("Resetting selected account due to timeout");
					_pendingFilterState.SelectedAccountId = 0;
				}
			}
			catch (Exception ex)
			{
				Logger?.LogError(ex, "Failed to load accounts for filter");
				_allAccounts = new List<Account>();
				Accounts = new List<Account>();

				// Ensure pending state remains consistent
				if (_pendingFilterState.SelectedAccountId > 0)
				{
					Logger?.LogWarning("Resetting selected account due to load failure");
					_pendingFilterState.SelectedAccountId = 0;
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
				Logger?.LogInformation("Loading symbols for filter...");

				// Add timeout to prevent infinite loading
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
				var symbols = await AccountDataService.GetSymbolProfilesAsync(null, cts.Token);

				// Only update if we got a valid result
				if (symbols != null)
				{
					_allSymbols = symbols;
					Symbols = _allSymbols; // Initially show all symbols
					Logger?.LogInformation("Successfully loaded {Count} symbols for filter", _allSymbols.Count);
				}
				else
				{
					Logger?.LogWarning("GetSymbolsAsync returned null");
					_allSymbols = new List<string>();
					Symbols = new List<string>();
				}
			}
			catch (OperationCanceledException)
			{
				Logger?.LogError("Timeout loading symbols for filter");
				_allSymbols = new List<string>();
				Symbols = new List<string>();

				// Ensure pending state remains consistent
				if (!string.IsNullOrEmpty(_pendingFilterState.SelectedSymbol))
				{
					Logger?.LogWarning("Resetting selected symbol due to timeout");
					_pendingFilterState.SelectedSymbol = "";
				}
			}
			catch (Exception ex)
			{
				Logger?.LogError(ex, "Failed to load symbols for filter");
				_allSymbols = new List<string>();
				Symbols = new List<string>();

				// Ensure pending state remains consistent
				if (!string.IsNullOrEmpty(_pendingFilterState.SelectedSymbol))
				{
					Logger?.LogWarning("Resetting selected symbol due to load failure");
					_pendingFilterState.SelectedSymbol = "";
				}
			}
			finally
			{
				_isLoadingSymbols = false;
			}
		}

		private void DetectCurrentDateRange()
		{
			if (_pendingFilterState == null) return;

			var today = DateOnly.FromDateTime(DateTime.Today);
			var startOfYear = DateOnly.FromDateTime(new DateTime(today.Year, 1, 1));

			// Check if current dates match predefined ranges
			if (_pendingFilterState.StartDate == startOfYear && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "YearToDate";
			}
			else if (_pendingFilterState.StartDate == today.AddDays(-7) && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "LastWeek";
			}
			else if (_pendingFilterState.StartDate == today.AddMonths(-1) && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "LastMonth";
			}
			else if (_pendingFilterState.StartDate == today.AddMonths(-3) && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "ThreeMonths";
			}
			else if (_pendingFilterState.StartDate == today.AddMonths(-6) && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "SixMonths";
			}
			else if (_pendingFilterState.StartDate == today.AddYears(-1) && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "OneYear";
			}
			else if (_pendingFilterState.StartDate == today.AddYears(-5) && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "FiveYear";
			}
			else if (_pendingFilterState.StartDate == GetMinDate() && _pendingFilterState.EndDate == today)
			{
				_currentDateRange = "Max";
			}
			else
			{
				_currentDateRange = null; // Custom date range
			}
		}

		private async Task SetDateRange(string range)
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			_currentDateRange = range;

			switch (range)
			{
				case "LastWeek":
					_pendingFilterState.StartDate = today.AddDays(-7);
					_pendingFilterState.EndDate = today;
					break;
				case "LastMonth":
					_pendingFilterState.StartDate = today.AddMonths(-1);
					_pendingFilterState.EndDate = today;
					break;
				case "ThreeMonths":
					_pendingFilterState.StartDate = today.AddMonths(-3);
					_pendingFilterState.EndDate = today;
					break;
				case "SixMonths":
					_pendingFilterState.StartDate = today.AddMonths(-6);
					_pendingFilterState.EndDate = today;
					break;
				case "YearToDate":
					_pendingFilterState.StartDate = DateOnly.FromDateTime(new DateTime(today.Year, 1, 1));
					_pendingFilterState.EndDate = today;
					break;
				case "OneYear":
					_pendingFilterState.StartDate = today.AddYears(-1);
					_pendingFilterState.EndDate = today;
					break;
				case "FiveYear":
					_pendingFilterState.StartDate = today.AddYears(-5);
					_pendingFilterState.EndDate = today;
					break;
				case "Max":
					_pendingFilterState.StartDate = GetMinDate();
					_pendingFilterState.EndDate = today;
					break;
			}

			// If apply button is not shown, apply changes immediately
			if (!ShowApplyButton && FilterState != null)
			{
				_pendingFilterState.ApplyTo(FilterState);
			}
		}

		private string GetDateRangeButtonClass(string range)
		{
			return range == _currentDateRange ? "btn-primary" : "btn-outline-primary";
		}

		private async Task OnStartDateChanged(ChangeEventArgs e)
		{
			if (DateOnly.TryParse(e.Value?.ToString(), out var date))
			{
				_pendingFilterState.StartDate = date;
				_currentDateRange = null; // Clear predefined range when custom date is set

				// If apply button is not shown, apply changes immediately
				if (!ShowApplyButton && FilterState != null)
				{
					FilterState.StartDate = date;
				}
			}
		}

		private async Task OnEndDateChanged(ChangeEventArgs e)
		{
			if (DateOnly.TryParse(e.Value?.ToString(), out var date))
			{
				_pendingFilterState.EndDate = date;
				_currentDateRange = null; // Clear predefined range when custom date is set

				// If apply button is not shown, apply changes immediately
				if (!ShowApplyButton && FilterState != null)
				{
					FilterState.EndDate = date;
				}
			}
		}

		private async Task OnAccountChanged(ChangeEventArgs e)
		{
			if (int.TryParse(e.Value?.ToString(), out var accountId))
			{
				_pendingFilterState.SelectedAccountId = accountId;

				// Update available Symbols based on selected account
				await UpdateFilteredOptionsAsync();
				StateHasChanged();

				// If apply button is not shown, apply changes immediately
				if (!ShowApplyButton && FilterState != null)
				{
					FilterState.SelectedAccountId = accountId;
				}
			}
		}

		private async Task OnSymbolChanged(ChangeEventArgs e)
		{
			if (e.Value != null)
			{
				_pendingFilterState.SelectedSymbol = e.Value.ToString() ?? "";

				// Update available accounts based on selected symbol
				await UpdateFilteredOptionsAsync();
				StateHasChanged();

				// If apply button is not shown, apply changes immediately
				if (!ShowApplyButton && FilterState != null)
				{
					FilterState.SelectedSymbol = _pendingFilterState.SelectedSymbol;
				}
			}
		}

		private async Task OnTransactionTypeChanged(ChangeEventArgs e)
		{
			if (e.Value != null)
			{
				_pendingFilterState.SelectedTransactionType = e.Value.ToString() ?? "";

				// If apply button is not shown, apply changes immediately
				if (!ShowApplyButton && FilterState != null)
				{
					FilterState.SelectedTransactionType = _pendingFilterState.SelectedTransactionType;
				}
			}
		}

		private async Task OnSearchTextChanged(ChangeEventArgs e)
		{
			if (e.Value != null)
			{
				_pendingFilterState.SearchText = e.Value.ToString() ?? "";

				// If apply button is not shown, apply changes immediately
				if (!ShowApplyButton && FilterState != null)
				{
					FilterState.SearchText = _pendingFilterState.SearchText;
				}
			}
		}

		private async Task OnSearchTextInput(ChangeEventArgs e)
		{
			if (e.Value != null)
			{
				var newSearchText = e.Value.ToString() ?? "";
				_pendingFilterState.SearchText = newSearchText;

				// Debounce search when apply button is not shown (immediate mode)
				if (!ShowApplyButton && FilterState != null)
				{
					// Clear existing timer
					_searchDebounceTimer?.Dispose();

					// Set new timer for 300ms debounce
					_searchDebounceTimer = new Timer(async _ =>
					{
						await InvokeAsync(() =>
						{
							FilterState.SearchText = newSearchText;
							StateHasChanged();
						});
					}, null, 300, Timeout.Infinite);
				}
			}
		}

		private async Task ApplyFilters()
		{
			if (FilterState != null)
			{
				_pendingFilterState.ApplyTo(FilterState);
				Logger?.LogInformation("Applied filter changes to FilterState");

				// Trigger the callback to collapse the hamburger menu
				if (OnFiltersApplied.HasDelegate)
				{
					await OnFiltersApplied.InvokeAsync();
				}
			}
		}

		private async Task ResetFilters()
		{
			if (FilterState != null)
			{
				_pendingFilterState = PendingFilterState.FromFilterState(FilterState);
				DetectCurrentDateRange();
				await UpdateFilteredOptionsAsync();
				StateHasChanged();
				Logger?.LogInformation("Reset pending filter changes");
			}
		}

		private bool HasPendingChanges => FilterState != null && _pendingFilterState.HasChanges(FilterState);

		public void Dispose()
		{
			_searchDebounceTimer?.Dispose();
		}

		private DateOnly GetMinDate()
		{
			return AccountDataService.GetMinDateAsync().Result;
		}
	}
}