using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Holdings : IDisposable
	{
		[Inject]
		private IHoldingsDataService HoldingsDataService { get; set; } = default!;

		[Inject]
		private NavigationManager Navigation { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		// View mode for the treemap
		private string ViewMode = "treemap";
		private List<HoldingDisplayModel> HoldingsList = [];
		private Config plotConfig = new();
		private Plotly.Blazor.Layout plotLayout = new();
		private IList<ITrace> plotData = [];

		// Loading state management
		private bool IsLoading { get; set; } = true;
		private bool HasError { get; set; }
		private string ErrorMessage { get; set; } = string.Empty;

		// Sorting state
		private string sortColumn = "CurrentValue";
		private bool sortAscending;

		private FilterState? _previousFilterState;
		private bool _disposed;

		protected override Task OnInitializedAsync()
		{
			// Subscribe to filter changes
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}

			return Task.CompletedTask;
		}

		protected override Task OnParametersSetAsync()
		{
			// Check if filter state has changed
			if (_previousFilterState == null || HasFilterStateChanged())
			{
				// Unsubscribe from old filter state
				if (_previousFilterState != null)
				{
					_previousFilterState.PropertyChanged -= OnFilterStateChanged;
				}

				// Subscribe to new filter state
				if (FilterState != null)
				{
					FilterState.PropertyChanged += OnFilterStateChanged;
				}

				_previousFilterState = FilterState;
				return LoadPortfolioDataAsync();
			}

			return Task.CompletedTask;
		}

		private bool HasFilterStateChanged()
		{
			if (_previousFilterState == null) return true;

			// For Holdings, we care about currency and account changes
			return _previousFilterState.StartDate != FilterState.StartDate ||
				   _previousFilterState.EndDate != FilterState.EndDate ||
				   _previousFilterState.SelectedAccountId != FilterState.SelectedAccountId;
		}

		private async void OnFilterStateChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (_disposed) return;

			Console.WriteLine($"Holdings OnFilterStateChanged - Property: {e.PropertyName}, Current accountId: {FilterState?.SelectedAccountId}");

			// Only reload when specific properties change
			if (e.PropertyName == nameof(FilterState.SelectedAccountId) ||
				e.PropertyName == nameof(FilterState.StartDate) ||
				e.PropertyName == nameof(FilterState.EndDate))
			{
				Console.WriteLine($"Filter change detected in Holdings - AccountId: {FilterState?.SelectedAccountId}");
				await LoadPortfolioDataAsync();
			}

			StateHasChanged();
		}

		private async Task LoadPortfolioDataAsync()
		{
			try
			{
				IsLoading = true;
				HasError = false;
				ErrorMessage = string.Empty;
				StateHasChanged(); // Update UI to show loading state
				await Task.Delay(0);

				HoldingsList = await LoadRealPortfolioDataAsync();
				SortHoldings(); // Sort after loading

				// Prepare chart data after loading
				await Task.Run(PrepareTreemapData);
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = ex.Message;
			}
			finally
			{
				IsLoading = false;
				StateHasChanged(); // Update UI when loading is complete
			}
		}

		private async Task<List<HoldingDisplayModel>> LoadRealPortfolioDataAsync()
		{
			try
			{
				return await (HoldingsDataService?.GetHoldingsAsync(FilterState.SelectedAccountId) ?? Task.FromResult(new List<HoldingDisplayModel>()));
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to load portfolio data: {ex.Message}", ex);
			}
		}

		private void PrepareTreemapData()
		{
			if (HoldingsList.Count == 0)
			{
				return;
			}

			var treemapTrace = new TreeMap
			{
				Labels = HoldingsList.Select(h => $"{h.Name} (GainLossPercentage {h.GainLossPercentage})").ToArray(),
				Values = [.. HoldingsList.Select(h => (object)h.CurrentValue.Amount)],
				Parents = HoldingsList.Select(h => "").ToArray(),
				Text = HoldingsList.Select(h => $"{h.Name}({h.Symbol})<br>{CurrencyDisplay.DisplaySignAndAmount(h.CurrentValue)}").ToArray(),
				TextInfo = Plotly.Blazor.Traces.TreeMapLib.TextInfoFlag.Text,
				BranchValues = Plotly.Blazor.Traces.TreeMapLib.BranchValuesEnum.Total,
				PathBar = new Plotly.Blazor.Traces.TreeMapLib.PathBar
				{
					Visible = false
				},
				Marker = new Plotly.Blazor.Traces.TreeMapLib.Marker
				{
					Colors = [.. HoldingsList.Select(h => (object)GetColorForGainLoss(h.GainLossPercentage))],
					Line = new Plotly.Blazor.Traces.TreeMapLib.MarkerLib.Line
					{
						Width = 2,
						Color = "#000000"
					}
				},
				TextFont = new Plotly.Blazor.Traces.TreeMapLib.TextFont
				{
					Size = 12,
					Color = "#000000"
				}
			};

			plotData = [treemapTrace];

			plotLayout = new Plotly.Blazor.Layout
			{
				Margin = new Plotly.Blazor.LayoutLib.Margin
				{
					T = 10,
					L = 10,
					R = 10,
					B = 10
				},
				AutoSize = true,
			};

			plotConfig = new Config
			{
				Responsive = true,
			};
		}

		private static string GetColorForGainLoss(decimal gainLossPercentage)
		{
			if (Math.Abs(gainLossPercentage) < 0.01m)
			{
				return "#e3f2fd"; // Light blue for neutral - more pleasing than gray
			}

			// Clamp the percentage to a reasonable range for color intensity
			const decimal maxAbs = 50m; // 50% gain/loss is max intensity
			var clamped = Math.Max(-maxAbs, Math.Min(maxAbs, gainLossPercentage));
			var intensity = (int)(Math.Min(Math.Abs(clamped) / maxAbs, 1m) * 255);

			if (clamped > 0)
			{
				// Green: from light green (#e8f5e8) to stronger green (#4caf50)
				int r = 232 - (int)(184 * (intensity / 255.0)); // fades from 232 to 76 (4c in hex)
				int g = 245 - (int)(70 * (intensity / 255.0));  // fades from 245 to 175 (af in hex)
				int b = 232 - (int)(152 * (intensity / 255.0)); // fades from 232 to 80 (50 in hex)
				return $"#{r:X2}{g:X2}{b:X2}";
			}
			else
			{
				// Red: from light red (#ffebee) to stronger red (#f44336)
				int r = 255 - (int)(11 * (intensity / 255.0));  // fades from 255 to 244 (f4 in hex)
				int g = 235 - (int)(168 * (intensity / 255.0)); // fades from 235 to 67 (43 in hex)
				int b = 238 - (int)(184 * (intensity / 255.0)); // fades from 238 to 54 (36 in hex)
				return $"#{r:X2}{g:X2}{b:X2}";
			}
		}

		// Add refresh method for manual data reload
		private async Task RefreshDataAsync()
		{
			await LoadPortfolioDataAsync();
		}

		private Money TotalValue => Money.Sum(HoldingsList.Select(h => h.CurrentValue));
		private Money TotalGainLoss => Money.Sum(HoldingsList.Select(h => h.GainLoss));
		private decimal TotalGainLossPercentage => TotalGainLoss.SafeDivide(TotalValue.Subtract(TotalGainLoss)).Amount;

		private Dictionary<string, decimal> SectorAllocation =>
			HoldingsList.GroupBy(h => h.Sector)
				   .ToDictionary(g => g.Key, g => g.Sum(h => h.Weight));

		private Dictionary<string, decimal> AssetClassAllocation =>
			HoldingsList.GroupBy(h => h.AssetClass)
				   .ToDictionary(g => g.Key, g => g.Sum(h => h.Weight));

		private void SortBy(string column)
		{
			if (sortColumn == column)
			{
				sortAscending = !sortAscending;
			}
			else
			{
				sortColumn = column;
				sortAscending = true;
			}
			SortHoldings();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "TODO, Sort logic")]
		private void SortHoldings()
		{
			switch (sortColumn)
			{
				case "Symbol":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.Symbol)] : [.. HoldingsList.OrderByDescending(h => h.Symbol)];
					break;
				case "Name":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.Name)] : [.. HoldingsList.OrderByDescending(h => h.Name)];
					break;
				case "Quantity":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.Quantity)] : [.. HoldingsList.OrderByDescending(h => h.Quantity)];
					break;
				case "AveragePrice":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.AveragePrice)] : [.. HoldingsList.OrderByDescending(h => h.AveragePrice)];
					break;
				case "CurrentPrice":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.CurrentPrice)] : [.. HoldingsList.OrderByDescending(h => h.CurrentPrice)];
					break;
				case "CurrentValue":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.CurrentValue)] : [.. HoldingsList.OrderByDescending(h => h.CurrentValue)];
					break;
				case "GainLoss":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.GainLoss)] : [.. HoldingsList.OrderByDescending(h => h.GainLoss)];
					break;
				case "GainLossPercentage":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.GainLossPercentage)] : [.. HoldingsList.OrderByDescending(h => h.GainLossPercentage)];
					break;
				case "Weight":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.Weight)] : [.. HoldingsList.OrderByDescending(h => h.Weight)];
					break;
				case "Sector":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.Sector)] : [.. HoldingsList.OrderByDescending(h => h.Sector)];
					break;
				case "AssetClass":
					HoldingsList = sortAscending ? [.. HoldingsList.OrderBy(h => h.AssetClass)] : [.. HoldingsList.OrderByDescending(h => h.AssetClass)];
					break;
				default:
					break;
			}
		}

		private void NavigateToHoldingDetail(string symbol)
		{
			Navigation?.NavigateTo($"/holding/{Uri.EscapeDataString(symbol)}");
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				// Unsubscribe from current filter state
				if (FilterState != null)
				{
					FilterState.PropertyChanged -= OnFilterStateChanged;
				}

				// Unsubscribe from previous filter state if it's different
				if (_previousFilterState != null && _previousFilterState != FilterState)
				{
					_previousFilterState.PropertyChanged -= OnFilterStateChanged;
				}

				_disposed = true;
			}
		}
	}
}