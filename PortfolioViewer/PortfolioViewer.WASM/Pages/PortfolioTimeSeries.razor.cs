using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
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

		// View mode for chart/table toggle
		protected string ViewMode { get; set; } = "chart";

		// Filters
		protected DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-6);
		protected DateTime EndDate { get; set; } = DateTime.Today;
		protected string SelectedCurrency { get; set; } = "EUR";
		protected int SelectedAccountId { get; set; } // New property for selected account
		protected List<Account> Accounts { get; set; } = new(); // New property for account list

		protected DateOnly MinDate { get; set; } = DateOnly.FromDayNumber(1);

		// State
		protected bool IsLoading { get; set; } = false;
		protected bool HasError { get; set; } = false;
		protected string ErrorMessage { get; set; } = string.Empty;
		protected List<PortfolioValueHistoryPoint> TimeSeriesData { get; set; } = new();
		protected List<TimeSeriesDisplayModel> TimeSeriesDisplayData { get; set; } = new();

		// Sorting state
		private string sortColumn = "Date";
		private bool sortAscending = false;

		// Plotly chart
		protected Config plotConfig = new();
		protected Plotly.Blazor.Layout plotLayout = new();
		protected IList<ITrace> plotData = new List<ITrace>();

		protected override async Task OnInitializedAsync()
		{
			MinDate = await HoldingsDataService.GetMinDateAsync();
			Accounts = await HoldingsDataService.GetAccountsAsync();
			SelectedAccountId = 0;
			
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
					EndDate,
					SelectedAccountId // Pass selected account
				) ?? new List<PortfolioValueHistoryPoint>();
				
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

		private async Task PrepareDisplayData()
		{
			if (TimeSeriesData.Count == 0)
			{
				TimeSeriesDisplayData = new List<TimeSeriesDisplayModel>();
				return;
			}

			var displayData = new List<TimeSeriesDisplayModel>();
			var targetCurrency = Currency.GetCurrency(SelectedCurrency);

			foreach (var point in TimeSeriesData)
			{
				var totalValue = await SumMoney(point.Value, point.Date, targetCurrency);
				var totalInvested = await SumMoney(point.Invested, point.Date, targetCurrency);
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
				ShowLegend = true,
				Legend = [new Plotly.Blazor.LayoutLib.Legend
				{
					Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.H,
				}]
			};
			plotConfig = new Config { Responsive = true };
		}

		private async Task<Money> SumMoney(Money[] values, DateOnly date, Currency targetCurrency)
		{
			if (CurrencyExchange == null)
			{
				return new Money(targetCurrency, 0);
			}

			if (values.Length == 0)
			{
				return new Money(targetCurrency, 0);
			}

			var convertedValues = new List<Money>(values.Length);
			foreach (var v in values)
			{
				var converted = await CurrencyExchange.ConvertMoney(v, targetCurrency, date);
				convertedValues.Add(converted);
			}

			return Money.Sum(convertedValues);
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
						? TimeSeriesDisplayData.OrderBy(d => d.Date).ToList() 
						: TimeSeriesDisplayData.OrderByDescending(d => d.Date).ToList();
					break;
				case "TotalValue":
					TimeSeriesDisplayData = sortAscending 
						? TimeSeriesDisplayData.OrderBy(d => d.TotalValue.Amount).ToList() 
						: TimeSeriesDisplayData.OrderByDescending(d => d.TotalValue.Amount).ToList();
					break;
				case "TotalInvested":
					TimeSeriesDisplayData = sortAscending 
						? TimeSeriesDisplayData.OrderBy(d => d.TotalInvested.Amount).ToList() 
						: TimeSeriesDisplayData.OrderByDescending(d => d.TotalInvested.Amount).ToList();
					break;
				case "GainLoss":
					TimeSeriesDisplayData = sortAscending 
						? TimeSeriesDisplayData.OrderBy(d => d.GainLoss.Amount).ToList() 
						: TimeSeriesDisplayData.OrderByDescending(d => d.GainLoss.Amount).ToList();
					break;
				case "GainLossPercentage":
					TimeSeriesDisplayData = sortAscending 
						? TimeSeriesDisplayData.OrderBy(d => d.GainLossPercentage).ToList() 
						: TimeSeriesDisplayData.OrderByDescending(d => d.GainLossPercentage).ToList();
					break;
				default:
					break;
			}
		}
	}
}
