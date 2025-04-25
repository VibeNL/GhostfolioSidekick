using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using Microsoft.AspNetCore.Components;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Home
	{
		[Inject]
		private PortfolioClient PortfolioClient { get; set; }

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
	}
}