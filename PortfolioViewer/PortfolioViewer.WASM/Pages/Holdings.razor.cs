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
				Labels = HoldingsList.Select(h => $"{h.Name} (GainLossPercentage {h.GainLossPercentage})").ToArray(),
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
			if (Math.Abs(gainLossPercentage) < 0.01m)
			{
				return "#808080"; // Gray for neutral
			}

			// Clamp the percentage to a reasonable range for color intensity
			const decimal maxAbs = 50m; // 50% gain/loss is max intensity
			var clamped = Math.Max(-maxAbs, Math.Min(maxAbs, gainLossPercentage));
			var intensity = (int)(Math.Min(Math.Abs(clamped) / maxAbs, 1m) * 255);

			if (clamped > 0)
			{
				// Green: from pastel (#ccffcc) to pure green (#00ff00)
				int r = 204 - (int)(204 * (intensity / 255.0)); // fades from 204 to 0
				int g = 255;
				int b = 204 - (int)(204 * (intensity / 255.0)); // fades from 204 to 0
				return $"#{r:X2}{g:X2}{b:X2}";
			}
			else
			{
				// Red: from pastel (#ffcccc) to pure red (#ff0000)
				int r = 255;
				int g = 204 - (int)(204 * (intensity / 255.0)); // fades from 204 to 0
				int b = 204 - (int)(204 * (intensity / 255.0)); // fades from 204 to 0
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