using GhostfolioSidekick.PortfolioViewer.WASM.Clients;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.Model;
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

		[Inject] private ISyncConfigurationService SyncConfigurationService { get; set; } = default!;

		private IJSObjectReference? mermaidmodule;
		private Timer? refreshTimer;

		private string CurrentAction = "Idle";
		private int Progress = 0;
		private bool IsSyncing = false;
		private DateTime? LastSyncTime = null;

		// Currency-related fields
		private string _selectedCurrency = "EUR";
		private string _lastSyncCurrency = string.Empty;
		private string _statusMessage = string.Empty;
		private bool _isError = false;

		private async Task StartSync()
		{
			await StartSync(false);
		}

		private async Task StartFullSync()
		{
			await StartSync(true);
		}

		private async Task StartSync(bool forceFullSync)
		{
			IsSyncing = true;
			CurrentAction = forceFullSync ? "Starting full sync..." : "Starting sync...";
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
				// Update the sync configuration service with selected currency
				var targetCurrency = Currency.GetCurrency(_selectedCurrency);
				SyncConfigurationService.TargetCurrency = targetCurrency;

				CurrentAction = $"Syncing data with server-side currency conversion to {_selectedCurrency}...";
				StateHasChanged();

				// Currency conversion is now handled on the server side
				await PortfolioClient.SyncPortfolio(progress, forceFullSync, targetCurrency);
				
				// Update the last sync time after successful completion
				var now = DateTime.Now;
				await SyncTrackingService.SetLastSyncTimeAsync(now);
				LastSyncTime = now;
				_lastSyncCurrency = _selectedCurrency;

				_statusMessage = $"Sync completed successfully! All records converted to {_selectedCurrency} on the server.";
				_isError = false;
			}
			catch (Exception ex)
			{
				_statusMessage = $"Sync failed: {ex.Message}";
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

		private async Task OnCurrencyChanged(ChangeEventArgs e)
		{
			_selectedCurrency = e.Value?.ToString() ?? "EUR";
			_statusMessage = $"Currency changed to {_selectedCurrency}. Changes will take effect on next sync.";
			_isError = false;
			StateHasChanged();
			
			// Clear message after 3 seconds
			await Task.Delay(3000);
			_statusMessage = string.Empty;
			StateHasChanged();
		}

		private void ResetCurrency()
		{
			_selectedCurrency = "EUR";
			_statusMessage = "Currency reset to EUR.";
			_isError = false;
			StateHasChanged();
		}

		private string GetTimeSinceLastSync()
		{
			if (!LastSyncTime.HasValue)
				return string.Empty;

			var timeSince = DateTime.Now - LastSyncTime.Value;
			
			if (timeSince.TotalMinutes < 1)
				return "just now";
			else if (timeSince.TotalHours < 1)
				return $"{(int)timeSince.TotalMinutes} minute{((int)timeSince.TotalMinutes != 1 ? "s" : "")} ago";
			else if (timeSince.TotalDays < 1)
				return $"{(int)timeSince.TotalHours} hour{((int)timeSince.TotalHours != 1 ? "s" : "")} ago";
			else
				return $"{(int)timeSince.TotalDays} day{((int)timeSince.TotalDays != 1 ? "s" : "")} ago";
		}

		private int GetDaysSinceLastSync()
		{
			if (!LastSyncTime.HasValue)
				return int.MaxValue;

			return (int)(DateTime.Now - LastSyncTime.Value).TotalDays;
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
			// Load the last sync time when the component initializes
			LastSyncTime = await SyncTrackingService.GetLastSyncTimeAsync();
			
			// Initialize currency from service
			_selectedCurrency = SyncConfigurationService.TargetCurrency.Symbol;
			
			// Subscribe to currency changes
			SyncConfigurationService.CurrencyChanged += OnServiceCurrencyChanged;
			
			// Start a timer to refresh the "time since last sync" display every minute
			refreshTimer = new Timer(async _ =>
			{
				await InvokeAsync(StateHasChanged);
			}, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
		}

		private void OnServiceCurrencyChanged(object? sender, Currency currency)
		{
			_selectedCurrency = currency.Symbol;
			InvokeAsync(StateHasChanged);
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
			
			// Unsubscribe from currency changes
			if (SyncConfigurationService != null)
			{
				SyncConfigurationService.CurrencyChanged -= OnServiceCurrencyChanged;
			}
		}
	}
}