using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        protected FilterState FilterStateInstance { get; set; } = new();
        
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