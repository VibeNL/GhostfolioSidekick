using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Components
{
	public partial class PrivacyValue : ComponentBase, IDisposable
	{
		[Parameter, EditorRequired] public string Value { get; set; } = string.Empty;

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
