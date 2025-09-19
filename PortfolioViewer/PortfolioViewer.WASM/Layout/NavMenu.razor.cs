using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class NavMenu : ComponentBase
    {
        private bool collapseNavMenu = true;

        [Parameter] public bool ShowFilters { get; set; } = false;
        [Parameter] public bool ShowDateFilters { get; set; } = false;
        [Parameter] public bool ShowAccountFilter { get; set; } = false;
        [Parameter] public bool ShowSymbolFilter { get; set; } = false;

        private string? NavMenuCssClass => collapseNavMenu ? "collapse" : "collapse show";

        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        private void CollapseNavMenu()
        {
            collapseNavMenu = true;
        }
    }
}