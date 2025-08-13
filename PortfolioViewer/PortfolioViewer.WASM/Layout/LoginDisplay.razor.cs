using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Layout
{
    public partial class LoginDisplay : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        public void BeginLogOut()
        {
            Navigation.NavigateToLogout("authentication/logout");
        }
    }
}