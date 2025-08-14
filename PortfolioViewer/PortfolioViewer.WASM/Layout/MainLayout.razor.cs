using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        protected FilterState FilterStateInstance { get; set; } = new();
        
        // Determine which filters to show based on current page
        private bool ShouldShowFilters => ShouldShowDateFilters || ShouldShowCurrencyFilter || ShouldShowAccountFilters || ShouldShowSymbolFilter;
        private bool ShouldShowDateFilters => CurrentPageSupportsFilters && (IsTimeSeriesPage || IsHoldingDetailPage || IsTransactionsPage);
        private bool ShouldShowCurrencyFilter => CurrentPageSupportsFilters;
        private bool ShouldShowAccountFilters => CurrentPageSupportsFilters && (IsTimeSeriesPage || IsTransactionsPage);
        private bool ShouldShowSymbolFilter => CurrentPageSupportsFilters && IsTransactionsPage;
        
        private bool CurrentPageSupportsFilters => IsTimeSeriesPage || IsHoldingDetailPage || IsHoldingsPage || IsTransactionsPage;
        private bool IsTimeSeriesPage => Navigation.Uri.Contains("/portfolio-timeseries");
        private bool IsHoldingDetailPage => Navigation.Uri.Contains("/holding/");
        private bool IsHoldingsPage => Navigation.Uri.Contains("/holdings");
        private bool IsTransactionsPage => Navigation.Uri.Contains("/transactions");

        protected override void OnInitialized()
        {
            // Subscribe to navigation changes
            Navigation.LocationChanged += OnLocationChanged;
        }

        private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            // Trigger re-render when location changes to update filter visibility
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}