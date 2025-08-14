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
            Console.WriteLine($"MainLayout OnInitialized - FilterState currency: {_filterState.SelectedCurrency}");
            
            // Subscribe to navigation changes
            Navigation.LocationChanged += OnLocationChanged;
        }

        protected override void OnParametersSet()
        {
            Console.WriteLine($"MainLayout OnParametersSet - Global currency: {GlobalSelectedCurrency}");
        }

        private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            // Only trigger StateHasChanged on location changes, don't update filter state
            InvokeAsync(StateHasChanged);
        }

        // Individual parameter change handlers that will be called by NavMenu EventCallbacks
        private async Task OnStartDateChanged(DateTime newStartDate)
        {
            Console.WriteLine($"MainLayout OnStartDateChanged - Old: {GlobalStartDate}, New: {newStartDate}");
            GlobalStartDate = newStartDate;
            await UpdateFilterStateAndNotify();
        }

        private async Task OnEndDateChanged(DateTime newEndDate)
        {
            Console.WriteLine($"MainLayout OnEndDateChanged - Old: {GlobalEndDate}, New: {newEndDate}");
            GlobalEndDate = newEndDate;
            await UpdateFilterStateAndNotify();
        }

        private async Task OnSelectedCurrencyChanged(string newCurrency)
        {
            Console.WriteLine($"MainLayout OnSelectedCurrencyChanged - Old: {GlobalSelectedCurrency}, New: {newCurrency}");
            GlobalSelectedCurrency = newCurrency;
            await UpdateFilterStateAndNotify();
        }

        private async Task OnSelectedAccountIdChanged(int newAccountId)
        {
            Console.WriteLine($"MainLayout OnSelectedAccountIdChanged - Old: {GlobalSelectedAccountId}, New: {newAccountId}");
            GlobalSelectedAccountId = newAccountId;
            await UpdateFilterStateAndNotify();
        }

        private async Task OnGlobalFiltersChanged()
        {
            Console.WriteLine($"OnGlobalFiltersChanged called - Global currency: {GlobalSelectedCurrency}, FilterState currency: {_filterState.SelectedCurrency}");
            await UpdateFilterStateAndNotify();
        }

        private async Task UpdateFilterStateAndNotify()
        {
            // Update the FilterState object to match the new global values
            _filterState.UpdateAll(GlobalStartDate, GlobalEndDate, GlobalSelectedCurrency, GlobalSelectedAccountId);
            Console.WriteLine($"After UpdateAll - FilterState currency: {_filterState.SelectedCurrency}");
            
            // Force a complete re-render
            await InvokeAsync(StateHasChanged);
            
            // Yield control to allow UI to update
            await Task.Yield();
            
            // Trigger another state change to ensure all components are updated
            await InvokeAsync(StateHasChanged);
            
            Console.WriteLine($"UpdateFilterStateAndNotify complete - Final FilterState currency: {_filterState.SelectedCurrency}");
        }

        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}