using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class RedirectToLogin : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        protected override void OnInitialized()
        {
            Navigation.NavigateTo("/login", replace: true);
        }
    }
}