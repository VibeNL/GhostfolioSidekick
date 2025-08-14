using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class NavMenu : ComponentBase
    {
        private bool collapseNavMenu = true;

        [Parameter] public bool ShowFilters { get; set; } = false;
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

        private string? NavMenuCssClass => collapseNavMenu ? "collapse" : "collapse show";

        protected override void OnInitialized()
        {
            Console.WriteLine($"NavMenu OnInitialized - Currency: {SelectedCurrency}");
        }

        protected override void OnParametersSet()
        {
            Console.WriteLine($"NavMenu OnParametersSet - Currency: {SelectedCurrency}");
        }

        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }
    }
}