using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class RedirectToLogin : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        protected override void OnInitialized()
        {
            Navigation.NavigateToLogin("authentication/login");
        }
    }
}