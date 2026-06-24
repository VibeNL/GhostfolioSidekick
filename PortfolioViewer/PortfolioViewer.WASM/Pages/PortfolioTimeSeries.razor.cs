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

				await PrepareDisplayData();
				await PrepareChartData();
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
				// point.Value is the sum of TotalValue from CalculatedSnapshot (holdings value only, no cash)
				// point.Balance is the cash balance
				// point.Invested is the total invested in holdings
				var assetValue = new Money(targetCurrency, point.Value);
				var totalInvested = new Money(targetCurrency, point.Invested);
				var balance = new Money(targetCurrency, point.Balance);
				var totalValue = assetValue.Add(balance);
				var gainLoss = assetValue.Subtract(totalInvested);
				var gainLossPercentage = totalInvested.Amount == 0 ? 0 : gainLoss.Amount / totalInvested.Amount;

				displayData.Add(new TimeSeriesDisplayModel
				{
					Date = point.Date,
					TotalValue = totalValue,
					AssetValue = assetValue,
					TotalInvested = totalInvested,
					Balance = balance,
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

			var sorted = TimeSeriesDisplayData.OrderBy(p => p.Date).ToList();

			List<object> valueList = [];
			List<object> assetValueList = [];
			List<object> investedList = [];
			List<object> balanceList = [];
			List<object> gainLossPercentageList = [];
			foreach (var p in sorted)
			{
				valueList.Add(p.TotalValue.Amount);
				assetValueList.Add(p.AssetValue.Amount);
				investedList.Add(p.TotalInvested.Amount);
				balanceList.Add(p.Balance.Amount);
				gainLossPercentageList.Add(p.GainLossPercentage);
			}

			var dates = sorted.Select(p => p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToArray();

			var valueTrace = new Scatter
			{
				X = dates,
				Y = valueList,
				Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
				Name = "Portfolio Value",
				Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#007bff", Width = 2 },
				Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#007bff", Size = 6 }
			};
			var assetValueTrace = new Scatter
			{
				X = dates,
				Y = assetValueList,
				Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
				Name = "Holdings Value",
				Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#17a2b8", Width = 2 },
				Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#17a2b8", Size = 6 }
			};
			var investedTrace = new Scatter
			{
				X = dates,
				Y = investedList,
				Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
				Name = "Invested Amount",
				Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#28a745", Width = 2 },
				Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#28a745", Size = 6 }
			};
			var balanceTrace = new Scatter
			{
				X = dates,
				Y = balanceList,
				Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
				Name = "Cash Balance",
				Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#fd7e14", Width = 2 },
				Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#fd7e14", Size = 6 }
			};
			var gainLossPercentageTrace = new Scatter
			{
				X = dates,
				Y = gainLossPercentageList,
				Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
				Name = "Gain/Loss %",
				Line = new Plotly.Blazor.Traces.ScatterLib.Line { Color = "#6f42c1", Width = 2 },
				Marker = new Plotly.Blazor.Traces.ScatterLib.Marker { Color = "#6f42c1", Size = 6 },
				YAxis = "y2"
			};

			plotData = [valueTrace, assetValueTrace, investedTrace, balanceTrace, gainLossPercentageTrace];
			var primaryCurrency = ServerConfigurationService?.PrimaryCurrency;
			plotLayout = new Plotly.Blazor.Layout
			{
				Title = new Plotly.Blazor.LayoutLib.Title { Text = "Portfolio Value Over Time" },
				XAxis = [new Plotly.Blazor.LayoutLib.XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Date" } }],
				YAxis =
				[
					new Plotly.Blazor.LayoutLib.YAxis { Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Value ({primaryCurrency?.Symbol ?? "$"})" } },
					new Plotly.Blazor.LayoutLib.YAxis
					{
						Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = "Gain/Loss %" },
						Overlaying = "y",
						Side = Plotly.Blazor.LayoutLib.YAxisLib.SideEnum.Right,
						TickFormat = ".2%"
					}
				],
				Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 60, R = 60, B = 40 },
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
				case "Balance":
					TimeSeriesDisplayData = sortAscending
						? [.. TimeSeriesDisplayData.OrderBy(d => d.Balance.Amount)]
						: [.. TimeSeriesDisplayData.OrderByDescending(d => d.Balance.Amount)];
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
