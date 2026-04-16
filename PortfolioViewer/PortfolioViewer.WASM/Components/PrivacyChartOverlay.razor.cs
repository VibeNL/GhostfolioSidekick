using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components
{
    public partial class PrivacyChartOverlay : ComponentBase, IDisposable
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void OnInitialized()
        {
            PrivacyModeService.OnChange += OnPrivacyModeChanged;
        }

        private void OnPrivacyModeChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            PrivacyModeService.OnChange -= OnPrivacyModeChanged;
        }
    }
}
