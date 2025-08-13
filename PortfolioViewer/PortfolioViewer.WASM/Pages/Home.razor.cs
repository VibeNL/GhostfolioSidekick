using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Home : ComponentBase, IDisposable
	{
		[Inject]
		private PortfolioClient PortfolioClient { get; set; }

		[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

		private IJSObjectReference? mermaidmodule;

		private string CurrentAction = "Idle";
		private int Progress = 0;
		private bool IsSyncing = false;

		private async Task StartSync()
		{
			IsSyncing = true;
			CurrentAction = "Starting sync...";
			Progress = 0;

			var progress = new Progress<(string action, int progress)>(update =>
			{
				CurrentAction = update.action;
				Progress = update.progress;
				StateHasChanged(); // Update the UI
			});

			try
			{
				await PortfolioClient.SyncPortfolio(progress);
			}
			finally
			{
				IsSyncing = false;
			}
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				try
				{
					mermaidmodule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/mermaidmodule.js");
					await mermaidmodule.InvokeVoidAsync("Initialize");
					await mermaidmodule.InvokeVoidAsync("Render", "mermaid");
				}
				catch
				{
					// Silently handle if the mermaid module is not available
				}
			}
			else
			{
				try
				{
					if (mermaidmodule != null)
					{
						await mermaidmodule.InvokeVoidAsync("Render", "mermaid");
					}
				}
				catch
				{
					// Silently handle any JS interop errors
				}
			}
		}

		public void Dispose()
		{
			mermaidmodule?.DisposeAsync();
		}
	}
}