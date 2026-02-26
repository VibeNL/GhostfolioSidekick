using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Home : ComponentBase, IDisposable
	{
		[Inject]
		private PortfolioClient PortfolioClient { get; set; } = default!;

		[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

		[Inject] private ISyncTrackingService SyncTrackingService { get; set; } = default!;

		[Inject] private IWakeLockService WakeLockService { get; set; } = default!;

		[Inject] private IVersionService VersionService { get; set; } = default!;

		[Inject] private HttpClient Http { get; set; } = default!;

		private IJSObjectReference? mermaidmodule;
		private Timer? refreshTimer;
		private Timer? versionCheckTimer;

		private string CurrentAction = "Idle";
		private int Progress;
		private bool IsSyncing;
		private DateTime? LastSyncTime;
		private bool IsWakeLockActive;

		private string _statusMessage = string.Empty;
		private bool _isError;

		private string ClientVersion = string.Empty;
		private string ServerVersion = string.Empty;
		private bool IsUpdateAvailable;

		private MigrationStatusViewModel? MigrationStatus;

		private bool IsLoading = true;
		private bool IsReloading;

		private async Task RefreshPage()
		{
			IsReloading = true;
			StateHasChanged();
			await JSRuntime.InvokeVoidAsync("forceBlazorReload");
		}

		private async Task StartFullSync()
		{
			await StartSync(true);
		}

		private async Task StartSync()
		{
			await StartSync(false);
		}

		private async Task StartSync(bool forceFullSync)
		{
			IsSyncing = true;
			CurrentAction = forceFullSync ? "Starting full sync..." : "Starting sync...";
			Progress = 0;
			_statusMessage = string.Empty;

			// Request wake lock to keep screen active during sync
			var wakeLockRequested = await WakeLockService.RequestWakeLockAsync();
			if (wakeLockRequested)
			{
				IsWakeLockActive = true;
				StateHasChanged();
			}

			var progress = new Progress<(string action, int progress)>(update =>
			{
				CurrentAction = update.action;
				Progress = update.progress;
				StateHasChanged(); // Update the UI
			});

			try
			{
				// Currency conversion is now handled on the server side
				await PortfolioClient.SyncPortfolio(progress, forceFullSync);

				// Update the last sync time after successful completion
				var now = DateTime.Now;
				await SyncTrackingService.SetLastSyncTimeAsync(now);
				LastSyncTime = now;

				_isError = false;
			}
			catch (Exception ex)
			{
				_statusMessage = $"Sync failed: {ex.Message}";
				_isError = true;
			}
			finally
			{
				// Release wake lock when sync is complete
				if (IsWakeLockActive)
				{
					await WakeLockService.ReleaseWakeLockAsync();
					IsWakeLockActive = false;
				}

				IsSyncing = false;
				CurrentAction = "Idle";
				Progress = 0;
				StateHasChanged();
			}
		}

		private async Task DeleteAllData()
		{
			// Show confirmation dialog
			var confirmed = await JSRuntime.InvokeAsync<bool>("confirm",
				"Are you sure you want to delete all data? This action cannot be undone and will remove all synced data from your local database.");

			if (!confirmed)
			{
				return;
			}

			IsSyncing = true;
			CurrentAction = "Deleting all data...";
			Progress = 0;
			_statusMessage = string.Empty;

			var progress = new Progress<(string action, int progress)>(update =>
			{
				CurrentAction = update.action;
				Progress = update.progress;
				StateHasChanged(); // Update the UI
			});

			try
			{
				await PortfolioClient.DeleteAllData(progress);

				// Clear the last sync time
				LastSyncTime = null;

				_statusMessage = "All data has been successfully deleted.";
				_isError = false;
			}
			catch (Exception ex)
			{
				_statusMessage = $"Delete failed: {ex.Message}";
				_isError = true;
			}
			finally
			{
				IsSyncing = false;
				CurrentAction = "Idle";
				Progress = 0;
				StateHasChanged();
			}
		}

		private string GetTimeSinceLastSync()
		{
			if (!LastSyncTime.HasValue)
			{
				return string.Empty;
			}

			var timeSince = DateTime.UtcNow - LastSyncTime.Value;

			if (timeSince.TotalMinutes < 1)
			{
				return "just now";
			}
			else if (timeSince.TotalHours < 1)
			{
				return $"{(int)timeSince.TotalMinutes} minute{((int)timeSince.TotalMinutes != 1 ? "s" : "")} ago";
			}
			else if (timeSince.TotalDays < 1)
			{
				return $"{(int)timeSince.TotalHours} hour{((int)timeSince.TotalHours != 1 ? "s" : "")} ago";
			}
			else
			{
				return $"{(int)timeSince.TotalDays} day{((int)timeSince.TotalDays != 1 ? "s" : "")} ago";
			}
		}

		private int GetDaysSinceLastSync()
		{
			if (!LastSyncTime.HasValue)
			{
				return int.MaxValue;
			}

			return (int)(DateTime.UtcNow - LastSyncTime.Value).TotalDays;
		}

		private string GetSyncButtonText()
		{
			if (IsSyncing)
				return "Syncing...";

			if (!LastSyncTime.HasValue)
				return "Start Initial Sync";

			var daysSince = GetDaysSinceLastSync();
			return daysSince <= 7 ? "Quick Sync" : "Start Sync";
		}

		protected override async Task OnInitializedAsync()
		{
			// Load version info
			ClientVersion = VersionService.ClientVersion;
			await CheckForUpdates();

			// Load migration status before sync
			try
			{
				MigrationStatus = await Http.GetFromJsonAsync<MigrationStatusViewModel>("/api/version/migration-status");
			}
			catch
			{
				// Ignore migration status errors (API may be unavailable)
			}

			// Load the last sync time when the component initializes
			LastSyncTime = await SyncTrackingService.GetLastSyncTimeAsync();

			refreshTimer = new Timer(async _ =>
			{
				await InvokeAsync(StateHasChanged);
			}, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

			// Check for updates every 5 minutes
			versionCheckTimer = new Timer(async _ =>
			{
				await CheckForUpdates();
				await InvokeAsync(StateHasChanged);
			}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

			IsLoading = false;
			StateHasChanged();
		}

		private async Task CheckForUpdates()
		{
			ServerVersion = await VersionService.GetServerVersionAsync() ?? string.Empty;
			IsUpdateAvailable = await VersionService.IsUpdateAvailableAsync();
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
			versionCheckTimer?.Dispose();
		}
	}
}