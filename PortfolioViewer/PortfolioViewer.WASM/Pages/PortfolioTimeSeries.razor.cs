using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.ComponentModel;
using System.Globalization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class PortfolioTimeSeries : ComponentBase, IDisposable
	{
		[Inject]
		private IHoldingsDataService HoldingsDataService { get; set; } = default!;

		[Inject]
		private IAccountDataService AccountDataService { get; set; } = default!;

		[Inject]
		private IServerConfigurationService ServerConfigurationService { get; set; } = default!;

		[CascadingParameter]
		private FilterState FilterState { get; set; } = new();

		// View mode for chart/table toggle
		protected string ViewMode { get; set; } = "chart";

		// Properties that read from cascaded filter state
		protected DateOnly StartDate => FilterState.StartDate;
		protected DateOnly EndDate => FilterState.EndDate;
		protected int SelectedAccountId => FilterState.SelectedAccountId;

		protected DateOnly MinDate { get; set; } = DateOnly.FromDayNumber(1);

		// State
		protected bool IsLoading { get; set; }
		protected bool HasError { get; set; }
		protected string ErrorMessage { get; set; } = string.Empty;
		protected List<PortfolioValueHistoryPoint> TimeSeriesData { get; set; } = [];
		protected List<TimeSeriesDisplayModel> TimeSeriesDisplayData { get; set; } = [];

		// Holdings data for risers and losers
		protected List<HoldingDisplayModel> HoldingsData { get; set; } = [];
		protected List<HoldingTimeRangePerformance> TopRisers { get; set; } = [];
		protected List<HoldingTimeRangePerformance> TopLosers { get; set; } = [];

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending;

		// Plotly chart
		protected Config plotConfig = new();
		protected Plotly.Blazor.Layout plotLayout = new();
		protected IList<ITrace> plotData = [];

		private FilterState? _previousFilterState;

		protected override async Task OnInitializedAsync()
		{
			MinDate = await AccountDataService.GetMinDateAsync();

			// Subscribe to filter changes
			if (FilterState != null)
			{
				FilterState.PropertyChanged += OnFilterStateChanged;
			}
		}

		protected override async Task OnParametersSetAsync()
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
				await LoadTimeSeriesAsync();
			}
		}

		private bool HasFilterStateChanged()
		{
			if (_previousFilterState == null) return true;

			return _previousFilterState.StartDate != FilterState.StartDate ||
				   _previousFilterState.EndDate != FilterState.EndDate ||
				   _previousFilterState.SelectedAccountId != FilterState.SelectedAccountId;
		}

		private async void OnFilterStateChanged(object? sender, PropertyChangedEventArgs e)
		{
			await InvokeAsync(async () =>
			{
				await LoadTimeSeriesAsync();
				StateHasChanged();
			});
		}

		protected async Task LoadTimeSeriesAsync()
		{
			IsLoading = true;
			HasError = false;
			ErrorMessage = string.Empty;
			StateHasChanged();
			await Task.Yield();
			try
			{
				if (HoldingsDataService == null)
				{
					throw new InvalidOperationException("HoldingsDataService is not initialized.");
				}

				// Load time series data
				TimeSeriesData = await HoldingsDataService.GetPortfolioValueHistoryAsync(
					StartDate,
					EndDate,
					SelectedAccountId
				) ?? [];

				// Load holdings data for risers and losers
				if (SelectedAccountId == 0)
				{
					HoldingsData = await HoldingsDataService.GetHoldingsAsync() ?? [];
				}
				else
				{
					HoldingsData = await HoldingsDataService.GetHoldingsAsync(SelectedAccountId) ?? [];
				}

				await PrepareDisplayData();
				await PrepareChartData();
				await PrepareRisersAndLosers();
			}
			catch (Exception ex)
			{
				HasError = true;
				ErrorMessage = ex.Message;
			}
			finally
			{
				IsLoading = false;
				StateHasChanged();
			}
		}

		private async Task PrepareRisersAndLosers()
		{
			var timeRangePerformances = new List<HoldingTimeRangePerformance>();

			// Calculate performance for each holding over the selected time range
			foreach (var holding in HoldingsData.Where(h => h.Quantity > 0 && h.CurrentValue.Amount > 0))
			{
				try
				{
					// Get price history for this holding over the selected time range
					var priceHistory = await HoldingsDataService.GetHoldingPriceHistoryAsync(
						holding.Symbol, 
						StartDate, 
						EndDate
					);

					if (priceHistory.Count != 0)
					{
						// Get start and end prices
						var startPrice = priceHistory.OrderBy(p => p.Date).First()?.Price;
						var endPrice = priceHistory.OrderByDescending(p => p.Date).First()?.Price;

						if (startPrice != null && endPrice != null && startPrice.Amount > 0)
						{
							// Calculate percentage change over the time range
							var percentageChange = (endPrice.Amount - startPrice.Amount) / startPrice.Amount;
							var absoluteChange = endPrice.Subtract(startPrice);

							timeRangePerformances.Add(new HoldingTimeRangePerformance
							{
								Symbol = holding.Symbol,
								Name = holding.Name,
								StartPrice = startPrice,
								EndPrice = endPrice,
								PercentageChange = percentageChange,
								AbsoluteChange = absoluteChange,
								CurrentValue = holding.CurrentValue,
								Quantity = holding.Quantity
							});
						}
					}
				}
				catch (Exception ex)
				{
					// Log error but continue with other holdings
					System.Diagnostics.Debug.WriteLine($"Error calculating performance for {holding.Symbol}: {ex.Message}");
				}
			}

			// Top 3 risers (highest percentage change over time range)
			TopRisers = [.. timeRangePerformances
				.Where(h => h.PercentageChange > 0)
				.OrderByDescending(h => h.PercentageChange)
				.Take(3)];

			// Top 3 losers (lowest percentage change over time range)
			TopLosers = [.. timeRangePerformances
				.Where(h => h.PercentageChange < 0)
				.OrderBy(h => h.PercentageChange)
				.Take(3)];
		}

		private Task PrepareDisplayData()
		{
			if (TimeSeriesData.Count == 0)
			{
				TimeSeriesDisplayData = [];
				return Task.CompletedTask;
			}

			var displayData = new List<TimeSeriesDisplayModel>();
			var targetCurrency = ServerConfigurationService?.PrimaryCurrency ?? Currency.GetCurrency("USD");

			foreach (var point in TimeSeriesData)
			{
				var totalValue = new Money(targetCurrency, point.Value);
				var totalInvested = new Money(targetCurrency, point.Invested);
				var gainLoss = totalValue.Subtract(totalInvested);
				var gainLossPercentage = totalInvested.Amount == 0 ? 0 : gainLoss.Amount / totalInvested.Amount;

				displayData.Add(new TimeSeriesDisplayModel
				{
					Date = point.Date,
					TotalValue = totalValue,
					TotalInvested = totalInvested,
					GainLoss = gainLoss,
					GainLossPercentage = gainLossPercentage
				});
			}

			TimeSeriesDisplayData = displayData;
			SortDisplayData();
			return Task.CompletedTask;
		}

		private Task PrepareChartData()
		{
			if (TimeSeriesData.Count == 0)
			{
				plotData = [];
				return Task.CompletedTask;
			}

			List<object> valueList = [];
			List<object> investedList = [];
			foreach (var p in TimeSeriesData)
			{
				valueList.Add(p.Value);
				investedList.Add(p.Invested);
			}

			var valueTrace = new Scatter
			{
				X = TimeSeriesData.Select(p => p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray(),
				Y = valueList,
				Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
				Name = "Portfolio Value",
				Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#007bff", Width = 2 },
				Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#007bff", Size = 6 }
			};
			var investedTrace = new Scatter
			{
				X = TimeSeriesData.Select(p => p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray(),
				Y = investedList,
				Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
				Name = "Invested Amount",
				Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#28a745", Width = 2 },
				Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#28a745", Size = 6 }
			};

			plotData = [valueTrace, investedTrace];
			var primaryCurrency = ServerConfigurationService?.PrimaryCurrency;
			plotLayout = new Plotly.Blazor.Layout
			{
				Title = new Plotly.Blazor.LayoutLib.Title { Text = "Portfolio Value Over Time" },
				XAxis = [new Plotly.Blazor.LayoutLib.XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Date" } }],
				YAxis = [new Plotly.Blazor.LayoutLib.YAxis { Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Value ({primaryCurrency?.Symbol ?? "$"})" } }],
				Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 60, R = 30, B = 40 },
				AutoSize = true,
				ShowLegend = true,
				Legend = [new Plotly.Blazor.LayoutLib.Legend
				{
					Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.H,
				}]
			};
			plotConfig = new Config { Responsive = true };
			return Task.CompletedTask;
		}

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
			SortDisplayData();
		}

		private void SortDisplayData()
		{
			switch (sortColumn)
			{
				case "Date":
					TimeSeriesDisplayData = sortAscending
						? [.. TimeSeriesDisplayData.OrderBy(d => d.Date)]
						: [.. TimeSeriesDisplayData.OrderByDescending(d => d.Date)];
					break;
				case "TotalValue":
					TimeSeriesDisplayData = sortAscending
						? [.. TimeSeriesDisplayData.OrderBy(d => d.TotalValue.Amount)]
						: [.. TimeSeriesDisplayData.OrderByDescending(d => d.TotalValue.Amount)];
					break;
				case "TotalInvested":
					TimeSeriesDisplayData = sortAscending
						? [.. TimeSeriesDisplayData.OrderBy(d => d.TotalInvested.Amount)]
						: [.. TimeSeriesDisplayData.OrderByDescending(d => d.TotalInvested.Amount)];
					break;
				case "GainLoss":
					TimeSeriesDisplayData = sortAscending
						? [.. TimeSeriesDisplayData.OrderBy(d => d.GainLoss.Amount)]
						: [.. TimeSeriesDisplayData.OrderByDescending(d => d.GainLoss.Amount)];
					break;
				case "GainLossPercentage":
					TimeSeriesDisplayData = sortAscending
						? [.. TimeSeriesDisplayData.OrderBy(d => d.GainLossPercentage)]
						: [.. TimeSeriesDisplayData.OrderByDescending(d => d.GainLossPercentage)];
					break;
				default:
					break;
			}
		}

		public void Dispose()
		{
			if (FilterState != null)
			{
				FilterState.PropertyChanged -= OnFilterStateChanged;
			}
			if (_previousFilterState != null)
			{
				_previousFilterState.PropertyChanged -= OnFilterStateChanged;
			}
		}
	}
}
