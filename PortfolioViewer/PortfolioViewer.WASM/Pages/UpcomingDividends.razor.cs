using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using System.Globalization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public partial class UpcomingDividends : ComponentBase
	{
		[Inject]
		private IUpcomingDividendsService DividendsService { get; set; } = default!;

		[Inject]
		private IServerConfigurationService ServerConfigurationService { get; set; } = default!;

		protected List<UpcomingDividendModel>? dividends;
		protected bool isLoading = true;

		protected IList<ITrace> chartData = [];
		protected Plotly.Blazor.Layout chartLayout = new();
		protected Config chartConfig = new();

		protected override async Task OnInitializedAsync()
		{
			isLoading = true;
			dividends = await DividendsService.GetUpcomingDividendsAsync();
			BuildChart();
			isLoading = false;
		}

		private void BuildChart()
		{
			if (dividends == null || dividends.Count == 0)
				return;

			var months = dividends
				.Select(d => (d.PaymentDate.Year, d.PaymentDate.Month))
				.Distinct()
				.OrderBy(m => m.Year).ThenBy(m => m.Month)
				.ToList();

			var monthLabels = months
				.Select(m => new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture))
				.ToArray();

			var symbols = dividends.Select(d => d.Symbol).Distinct().OrderBy(s => s).ToList();
			var traces = new List<ITrace>();

			foreach (var symbol in symbols)
			{
				var y = months
					.Select(m => (object)dividends
						.Where(d => d.Symbol == symbol && d.PaymentDate.Year == m.Year && d.PaymentDate.Month == m.Month)
						.Sum(d => d.AmountPrimaryCurrency))
					.ToArray();

				traces.Add(new Bar
				{
					X = monthLabels,
					Y = y,
					Name = symbol
				});
			}

			chartData = traces;

			var currencySymbol = ServerConfigurationService.PrimaryCurrency.Symbol;
			chartLayout = new Plotly.Blazor.Layout
			{
				Title = new Plotly.Blazor.LayoutLib.Title { Text = "Expected Monthly Dividends" },
				XAxis = [new Plotly.Blazor.LayoutLib.XAxis { Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title { Text = "Month" } }],
				YAxis = [new Plotly.Blazor.LayoutLib.YAxis { Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title { Text = $"Amount ({currencySymbol})" } }],
				BarMode = Plotly.Blazor.LayoutLib.BarModeEnum.Stack,
				Margin = new Plotly.Blazor.LayoutLib.Margin { T = 40, L = 60, R = 30, B = 40 },
				AutoSize = true,
				ShowLegend = true,
				Legend =
				[
					new Plotly.Blazor.LayoutLib.Legend
					{
						Orientation = Plotly.Blazor.LayoutLib.LegendLib.OrientationEnum.H,
					}
				]
			};
			chartConfig = new Config { Responsive = true };
		}
	}
}
