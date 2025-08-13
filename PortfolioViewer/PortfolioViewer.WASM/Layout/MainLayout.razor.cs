using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // Global filter state
        private DateTime GlobalStartDate = DateTime.Today.AddMonths(-6);
        private DateTime GlobalEndDate = DateTime.Today;
        private string GlobalSelectedCurrency = "EUR";
        private int GlobalSelectedAccountId = 0;

        private FilterState _filterState = new();
        
        // Determine which filters to show based on current page
        private bool ShouldShowFilters => ShouldShowDateFilters || ShouldShowCurrencyFilter || ShouldShowAccountFilters;
        private bool ShouldShowDateFilters => CurrentPageSupportsFilters && (IsTimeSeriesPage || IsHoldingDetailPage);
        private bool ShouldShowCurrencyFilter => CurrentPageSupportsFilters;
        private bool ShouldShowAccountFilters => CurrentPageSupportsFilters && IsTimeSeriesPage;
        
        private bool CurrentPageSupportsFilters => IsTimeSeriesPage || IsHoldingDetailPage || IsHoldingsPage;
        private bool IsTimeSeriesPage => Navigation.Uri.Contains("/portfolio-timeseries");
        private bool IsHoldingDetailPage => Navigation.Uri.Contains("/holding/");
        private bool IsHoldingsPage => Navigation.Uri.Contains("/holdings");

        protected override void OnInitialized()
        {
            // Initialize the filter state with current global values
            _filterState.UpdateAll(GlobalStartDate, GlobalEndDate, GlobalSelectedCurrency, GlobalSelectedAccountId);
            
            // Subscribe to navigation changes
            Navigation.LocationChanged += OnLocationChanged;
        }

        private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            // Only trigger StateHasChanged on location changes, don't update filter state
            InvokeAsync(StateHasChanged);
        }

        private async Task OnGlobalFiltersChanged()
        {
            // This method is called when filters in NavMenu/CommonFilters change
            // The global variables are already updated through two-way binding
            
            // Update the FilterState object to match the new global values
            _filterState.UpdateAll(GlobalStartDate, GlobalEndDate, GlobalSelectedCurrency, GlobalSelectedAccountId);
            
            // Force a complete re-render
            await InvokeAsync(StateHasChanged);
            
            // Yield control to allow UI to update
            await Task.Yield();
            
            // Trigger another state change to ensure all components are updated
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}