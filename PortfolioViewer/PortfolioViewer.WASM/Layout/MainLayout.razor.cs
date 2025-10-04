using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ITransactionService TransactionService { get; set; } = default!;

        protected FilterState FilterStateInstance { get; set; } = new();
        private List<string>? _cachedTransactionTypes;

		// Determine which filters to show based on current page
		private bool ShouldShowFilters => ShouldShowDateFilters || ShouldShowAccountFilters || ShouldShowSymbolFilter || ShouldShowTransactionTypeFilter || ShouldShowSearchFilter;
        private bool ShouldShowDateFilters => CurrentPageSupportsFilters && (IsTimeSeriesPage || IsHoldingDetailPage || IsTransactionsPage || IsAccountsPage);
        private bool ShouldShowAccountFilters => CurrentPageSupportsFilters && (IsTimeSeriesPage || IsTransactionsPage || IsHoldingsPage);
        private bool ShouldShowSymbolFilter => CurrentPageSupportsFilters && IsTransactionsPage;
        private bool ShouldShowTransactionTypeFilter => CurrentPageSupportsFilters && IsTransactionsPage;
        private bool ShouldShowSearchFilter => CurrentPageSupportsFilters && IsTransactionsPage;
        
        private bool CurrentPageSupportsFilters => IsTimeSeriesPage || IsHoldingDetailPage || IsHoldingsPage || IsTransactionsPage || IsAccountsPage;
        private bool IsTimeSeriesPage => Navigation.Uri.Contains("/portfolio-timeseries");
        private bool IsHoldingDetailPage => Navigation.Uri.Contains("/holding/");
        private bool IsHoldingsPage => Navigation.Uri.Contains("/holdings");
        private bool IsTransactionsPage => Navigation.Uri.Contains("/transactions");
        private bool IsAccountsPage => Navigation.Uri.Contains("/accounts");

        // Get transaction types for filtering
        private List<string>? TransactionTypes => IsTransactionsPage ? _cachedTransactionTypes : null;

        protected override async Task OnInitializedAsync()
        {
            // Subscribe to navigation changes
            Navigation.LocationChanged += OnLocationChanged;
            
            // Load transaction types for filtering
            if (IsTransactionsPage && _cachedTransactionTypes == null)
            {
                await LoadTransactionTypesAsync();
            }
        }

        private async void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            // Load transaction types if navigating to transactions page
            if (IsTransactionsPage && _cachedTransactionTypes == null)
            {
                await LoadTransactionTypesAsync();
            }
            
            // Trigger re-render when location changes to update filter visibility
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadTransactionTypesAsync()
        {
            try
            {
                _cachedTransactionTypes = await TransactionService.GetTransactionTypesAsync();
            }
            catch (Exception)
            {
                // Fallback to static types if service call fails
                _cachedTransactionTypes =
				[
					"Buy", "Sell", "Dividend", "Deposit", "Withdrawal", 
                    "Fee", "Interest", "Receive", "Send", "Staking Reward", "Gift"
                ];
            }
            StateHasChanged();
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}