using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Home : ComponentBase, IDisposable
	{
		[Inject]
		private PortfolioClient PortfolioClient { get; set; }

		[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

		[Inject] private ISyncTrackingService SyncTrackingService { get; set; } = default!;

		private IJSObjectReference? mermaidmodule;
		private Timer? refreshTimer;

		private string CurrentAction = "Idle";
		private int Progress = 0;
		private bool IsSyncing = false;
		private DateTime? LastSyncTime = null;

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
				
				// Update the last sync time after successful completion
				var now = DateTime.Now;
				await SyncTrackingService.SetLastSyncTimeAsync(now);
				LastSyncTime = now;
			}
			finally
			{
				IsSyncing = false;
			}
		}

		protected override async Task OnInitializedAsync()
		{
			// Load the last sync time when the component initializes
			LastSyncTime = await SyncTrackingService.GetLastSyncTimeAsync();
			
			// Start a timer to refresh the "time since last sync" display every minute
			refreshTimer = new Timer(async _ =>
			{
				await InvokeAsync(StateHasChanged);
			}, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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
			refreshTimer?.Dispose();
		}
	}
}