using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Portfolio
{
	/// <summary>
	/// AI-callable functions that answer questions about the user's portfolio.
	/// Uses <see cref="IServiceScopeFactory"/> so that scoped data services can be
	/// safely resolved from this singleton-lifetime object.
	/// </summary>
	public class PortfolioAgentFunction(IServiceScopeFactory scopeFactory)
	{
		[Description("Returns a summary of all current holdings in the portfolio including name, quantity, current value, average buy price, current price, gain/loss, weight, sector and asset class.")]
		public async Task<string> GetHoldings(
			[Description("Optional symbol filter (e.g. 'AAPL'). Leave empty to get all holdings.")] string? symbolFilter = null)
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			var service = scope.ServiceProvider.GetRequiredService<IHoldingsDataService>();
			var holdings = await service.GetHoldingsAsync();

			if (holdings.Count == 0)
			{
				return "No holdings found in the portfolio.";
			}

			var filtered = string.IsNullOrWhiteSpace(symbolFilter)
				? holdings
				: holdings.Where(h =>
					h.Symbols.Any(s => s.Contains(symbolFilter, StringComparison.OrdinalIgnoreCase)) ||
					h.Name.Contains(symbolFilter, StringComparison.OrdinalIgnoreCase)).ToList();

			if (filtered.Count == 0)
			{
				return $"No holdings matching '{symbolFilter}' found.";
			}

			var sb = new StringBuilder();
			sb.AppendLine($"Portfolio holdings ({filtered.Count} positions):");
			foreach (var h in filtered)
			{
				sb.AppendLine(h.ToString());
			}

			return sb.ToString();
		}

		[Description("Returns the current total portfolio value, total invested amount, total gain/loss and gain/loss percentage across all accounts.")]
		public async Task<string> GetPortfolioSummary()
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			var holdingsService = scope.ServiceProvider.GetRequiredService<IHoldingsDataService>();

			var holdings = await holdingsService.GetHoldingsAsync();

			if (holdings.Count == 0)
			{
				return "No portfolio data available.";
			}

			var totalCurrentValue = holdings.Sum(h => h.CurrentValue.Amount);
			var currency = holdings.FirstOrDefault()?.Currency ?? "?";
			var totalGainLoss = holdings.Sum(h => h.GainLoss.Amount);
			var weightedGainPct = holdings.Count > 0
				? holdings.Average(h => h.GainLossPercentage)
				: 0m;

			var sb = new StringBuilder();
			sb.AppendLine("Portfolio Summary:");
			sb.AppendLine($"  Total current value : {totalCurrentValue:N2} {currency}");
			sb.AppendLine($"  Total gain/loss     : {totalGainLoss:N2} {currency}");
			sb.AppendLine($"  Average gain/loss % : {weightedGainPct:N2}%");
			sb.AppendLine($"  Number of positions : {holdings.Count}");

			return sb.ToString();
		}

		[Description("Returns upcoming dividend payments for holdings in the portfolio, including ex-date, payment date, expected amount and whether the dividend is predicted or confirmed.")]
		public async Task<string> GetUpcomingDividends()
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			var service = scope.ServiceProvider.GetRequiredService<IUpcomingDividendsService>();
			var dividends = await service.GetUpcomingDividendsAsync();

			if (dividends.Count == 0)
			{
				return "No upcoming dividends found.";
			}

			var sb = new StringBuilder();
			sb.AppendLine($"Upcoming dividends ({dividends.Count} entries):");
			foreach (var d in dividends.OrderBy(d => d.ExDate))
			{
				var predicted = d.IsPredicted ? " [predicted]" : string.Empty;
				var amount = d.AmountPrimaryCurrency.HasValue
					? $"{d.AmountPrimaryCurrency.Value:N2} {d.PrimaryCurrency}"
					: $"{d.Amount:N2} {d.Currency}";
				sb.AppendLine($"  {d.CompanyName} ({d.Symbol}){predicted}: ex-date {d.ExDate:yyyy-MM-dd}, payment {d.PaymentDate:yyyy-MM-dd}, amount {amount}, qty {d.Quantity:N2}");
			}

			return sb.ToString();
		}

		[Description("Returns the portfolio value history (time series) for a given date range so the user can understand performance over time. Dates must be in yyyy-MM-dd format.")]
		public async Task<string> GetPortfolioPerformance(
			[Description("Start date in yyyy-MM-dd format (e.g. '2024-01-01'). Defaults to one year ago.")] string? startDate = null,
			[Description("End date in yyyy-MM-dd format (e.g. '2025-01-01'). Defaults to today.")] string? endDate = null)
		{
			var start = ParseDateOrDefault(startDate, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)));
			var end = ParseDateOrDefault(endDate, DateOnly.FromDateTime(DateTime.UtcNow));

			await using var scope = scopeFactory.CreateAsyncScope();
			var service = scope.ServiceProvider.GetRequiredService<IHoldingsDataService>();
			var history = await service.GetPortfolioValueHistoryAsync(start, end, null);

			if (history.Count == 0)
			{
				return $"No portfolio history data found between {start} and {end}.";
			}

			var first = history.First();
			var last = history.Last();
			var change = last.Value - first.Value;
			var changePct = first.Value != 0 ? change / first.Value * 100m : 0m;

			var sb = new StringBuilder();
			sb.AppendLine($"Portfolio performance from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}:");
			sb.AppendLine($"  Start value  : {first.Value:N2}");
			sb.AppendLine($"  End value    : {last.Value:N2}");
			sb.AppendLine($"  Change       : {change:N2} ({changePct:N2}%)");
			sb.AppendLine($"  Total invested (end): {last.Invested:N2}");

			// Include monthly snapshots so the model can see the trend
			sb.AppendLine("  Monthly snapshots (last day of each month):");
			var monthly = history
				.GroupBy(p => new { p.Date.Year, p.Date.Month })
				.Select(g => g.Last())
				.OrderBy(p => p.Date);

			foreach (var point in monthly)
			{
				sb.AppendLine($"    {point.Date:yyyy-MM-dd}  value={point.Value:N2}  invested={point.Invested:N2}");
			}

			return sb.ToString();
		}

		private static DateOnly ParseDateOrDefault(string? input, DateOnly fallback)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return fallback;
			}

			return DateOnly.TryParseExact(input, "yyyy-MM-dd", out var parsed) ? parsed : fallback;
		}
	}
}
