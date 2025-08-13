using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
    public partial class Authentication : ComponentBase
    {
        [Parameter] public string? Action { get; set; }
    }
}