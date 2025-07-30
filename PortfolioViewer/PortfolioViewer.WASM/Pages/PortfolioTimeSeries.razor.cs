using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.Globalization;
using System.Collections.Generic;
using GhostfolioSidekick.Database.Repository;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class PortfolioTimeSeries : ComponentBase
	{
		[Inject]
		private IHoldingsDataService? HoldingsDataService { get; set; }

		[Inject]
		private ICurrencyExchange? CurrencyExchange { get; set; }

		// Filters
		protected DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-6);
		protected DateTime EndDate { get; set; } = DateTime.Today;
		protected string SelectedCurrency { get; set; } = "EUR";

		// State
		protected bool IsLoading { get; set; } = false;
		protected bool HasError { get; set; } = false;
		protected string ErrorMessage { get; set; } = string.Empty;
		protected List<PortfolioValueHistoryPoint> TimeSeriesData { get; set; } = new();

		// Plotly chart
		protected Config plotConfig = new();
		protected Plotly.Blazor.Layout plotLayout = new();
		protected IList<ITrace> plotData = new List<ITrace>();

		protected override async Task OnInitializedAsync()
		{
			await LoadTimeSeriesAsync();
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
				if (HoldingsDataService == null || CurrencyExchange == null)
				{
					throw new InvalidOperationException("HoldingsDataService or CurrencyExchange is not initialized.");
				}

				var currency = Currency.GetCurrency(SelectedCurrency);
				TimeSeriesData = await HoldingsDataService.GetPortfolioValueHistoryAsync(
					currency,
					StartDate,
					EndDate
				) ?? new List<PortfolioValueHistoryPoint>();
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

		private async Task PrepareChartData()
		{
			if (TimeSeriesData.Count == 0)
			{
				plotData = new List<ITrace>();
				return;
			}

			List<object> valueList = new();
			List<object> investedList = new();
			foreach (var p in TimeSeriesData)
			{
				valueList.Add(await Sum(p.Value, p.Date, CurrencyExchange));
				investedList.Add(await Sum(p.Invested, p.Date, CurrencyExchange));
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

			plotData = new List<ITrace> { valueTrace, investedTrace };
			plotLayout = new Plotly.Blazor.Layout
			{
				Title = new Plotly.Blazor.LayoutLib.Title { Text = "Portfolio Value Over Time" },
				XAxis = new List<Plotly.Blazor.LayoutLib.XAxis> { new Plotly.Blazor.LayoutLib.XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Date" } } },
				YAxis = new List<Plotly.Blazor.LayoutLib.YAxis> { new Plotly.Blazor.LayoutLib.YAxis { Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Value ({SelectedCurrency})" } } },
				Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 60, R = 30, B = 40 },
				AutoSize = true,
			};
			plotConfig = new Config { Responsive = true };
		}

		private async Task<object> Sum(Money[] value, DateOnly date, ICurrencyExchange? currencyExchange)
		{
			var targetCurrency = Currency.GetCurrency(SelectedCurrency);
			if (currencyExchange == null)
			{
				return 0;
			}

			if (value.Length == 0)
			{
				return 0;
			}

			var convertedValues = new List<Money>(value.Length);
			foreach (var v in value)
			{
				var converted = await currencyExchange.ConvertMoney(v, targetCurrency, date);
				convertedValues.Add(converted);
			}

			var sum = Money.Sum(convertedValues);
			return sum.Amount;
		}
	}
}
