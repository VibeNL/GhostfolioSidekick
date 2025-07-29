using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class Holdings
	{
		[Inject]
		private IHoldingsDataService? HoldingsDataService { get; set; }
		
		// View mode for the treemap
		private string ViewMode = "treemap";
		private List<HoldingDisplayModel> HoldingsList = new();
		private Config plotConfig = new();
		private Plotly.Blazor.Layout plotLayout = new();
		private IList<ITrace> plotData = new List<ITrace>();

		// Loading state management
		private bool IsLoading { get; set; } = true;
		private bool HasError { get; set; } = false;
		private string ErrorMessage { get; set; } = string.Empty;

		// Sorting state
		private string sortColumn = "CurrentValue";
		private bool sortAscending = false;

		protected override async Task OnInitializedAsync()
		{
			await LoadPortfolioDataAsync();
		}

		private async Task LoadPortfolioDataAsync()
		{
			try
			{
				IsLoading = true;
				HasError = false;
				ErrorMessage = string.Empty;
				StateHasChanged(); // Update UI to show loading state

				// Yield control to allow UI to update
				await Task.Yield();

				HoldingsList = await LoadRealPortfolioDataAsync();
				SortHoldings(); // Sort after loading

				// Prepare chart data after loading
				await Task.Run(() => PrepareTreemapData());
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
				return await HoldingsDataService?.GetHoldingsAsync(Model.Currency.EUR) ?? new List<HoldingDisplayModel>();
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
				Labels = HoldingsList.Select(h => h.Name).ToArray(),
				Values = HoldingsList.Select(h => (object)h.CurrentValue.Amount).ToList(),
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
					Colors = HoldingsList.Select(h => (object)GetColorForGainLoss(h.GainLossPercentage)).ToList(),
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

			plotData = new List<ITrace> { treemapTrace };

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

		private object GetColorForGainLoss(decimal gainLossPercentage)
		{
			if (gainLossPercentage == 0)
			{
				return "#808080"; // Gray for neutral
			}

			// Normalize the gain/loss percentage to a range of -1 to 1
			decimal clamped = Math.Clamp(gainLossPercentage, -1, 1);

			// Apply quadratic scaling for a faster color transition
			decimal scaled = clamped >= 0 ? clamped * clamped : -1 * (clamped * clamped);

			if (scaled >= 0)
			{
				// Green scale: from #ffeaea (very light green) to #28a745 (strong green)
				// Interpolate between (255, 255, 234) and (40, 167, 69)
				int r = (int)(255 - (215 * scaled)); // 255 -> 40
				int g = (int)(255 - (88 * scaled));  // 255 -> 167
				int b = (int)(234 - (165 * scaled)); // 234 -> 69
				var green = $"#{r:X2}{g:X2}{b:X2}";
				return green;
			}
			else
			{
				// Red scale: from #eaffea (very light red) to #dc3545 (strong red)
				// Interpolate between (234, 255, 234) and (220, 53, 69)
				scaled = Math.Abs(scaled);
				int r = (int)(234 - (14 * scaled));  // 234 -> 220
				int g = (int)(255 - (202 * scaled)); // 255 -> 53
				int b = (int)(234 - (165 * scaled)); // 234 -> 69
				var red = $"#{r:X2}{g:X2}{b:X2}";
				return red;
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

		private void SortHoldings()
		{
			switch (sortColumn)
			{
				case "Symbol":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.Symbol).ToList() : HoldingsList.OrderByDescending(h => h.Symbol).ToList();
					break;
				case "Name":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.Name).ToList() : HoldingsList.OrderByDescending(h => h.Name).ToList();
					break;
				case "Quantity":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.Quantity).ToList() : HoldingsList.OrderByDescending(h => h.Quantity).ToList();
					break;
				case "AveragePrice":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.AveragePrice).ToList() : HoldingsList.OrderByDescending(h => h.AveragePrice).ToList();
					break;
				case "CurrentPrice":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.CurrentPrice).ToList() : HoldingsList.OrderByDescending(h => h.CurrentPrice).ToList();
					break;
				case "CurrentValue":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.CurrentValue).ToList() : HoldingsList.OrderByDescending(h => h.CurrentValue).ToList();
					break;
				case "GainLoss":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.GainLoss).ToList() : HoldingsList.OrderByDescending(h => h.GainLoss).ToList();
					break;
				case "GainLossPercentage":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.GainLossPercentage).ToList() : HoldingsList.OrderByDescending(h => h.GainLossPercentage).ToList();
					break;
				case "Weight":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.Weight).ToList() : HoldingsList.OrderByDescending(h => h.Weight).ToList();
					break;
				case "Sector":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.Sector).ToList() : HoldingsList.OrderByDescending(h => h.Sector).ToList();
					break;
				case "AssetClass":
					HoldingsList = sortAscending ? HoldingsList.OrderBy(h => h.AssetClass).ToList() : HoldingsList.OrderByDescending(h => h.AssetClass).ToList();
					break;
				default:
					break;
			}
		}
	}
}