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
	/// Output is intentionally compact to stay within the model's token budget.
	/// </summary>
	public class PortfolioAgentFunction(IServiceScopeFactory scopeFactory)
	{
		private const int DefaultTopHoldings = 10;
		private const int MaxDetailHoldings = 20;

		[Description("Returns the top holdings by value (default top 10, max 20). Use symbol_filter to look up a specific holding by name or ticker. For a full portfolio overview use get_portfolio_summary first.")]
		public async Task<string> GetHoldings(
			[Description("Optional symbol or name filter (e.g. 'AAPL' or 'Apple'). Leave empty to get the top holdings by value.")] string? symbolFilter = null,
			[Description("How many holdings to return when no filter is set. Default 10, max 20.")] int count = DefaultTopHoldings)
		{
			count = Math.Clamp(count, 1, MaxDetailHoldings);

			await using var scope = scopeFactory.CreateAsyncScope();
			var service = scope.ServiceProvider.GetRequiredService<IHoldingsDataService>();
			var holdings = await service.GetHoldingsAsync();

			if (holdings.Count == 0)
			{
				return "No holdings found in the portfolio.";
			}

			// If a filter is provided, match on symbol or name
			if (!string.IsNullOrWhiteSpace(symbolFilter))
			{
				var filtered = holdings
					.Where(h =>
						h.Symbols.Any(s => s.Contains(symbolFilter, StringComparison.OrdinalIgnoreCase)) ||
						h.Name.Contains(symbolFilter, StringComparison.OrdinalIgnoreCase))
					.ToList();

				return filtered.Count == 0
					? $"No holdings matching '{symbolFilter}' found."
					: FormatHoldings(filtered, $"Holdings matching '{symbolFilter}'");
			}

			// Default: top N by current value
			var top = holdings
				.OrderByDescending(h => h.CurrentValue.Amount)
				.Take(count)
				.ToList();

			var suffix = holdings.Count > count
				? $" (showing top {count} of {holdings.Count} — use get_holdings with a higher count or symbol_filter for others)"
				: string.Empty;

			return FormatHoldings(top, $"Top {top.Count} holdings by value{suffix}");
		}

		[Description("Returns the current total portfolio value, total invested, total gain/loss, gain/loss percentage, number of positions, top 5 winners and top 5 losers.")]
		public async Task<string> GetPortfolioSummary()
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			var service = scope.ServiceProvider.GetRequiredService<IHoldingsDataService>();
			var holdings = await service.GetHoldingsAsync();

			if (holdings.Count == 0)
			{
				return "No portfolio data available.";
			}

			var totalValue = holdings.Sum(h => h.CurrentValue.Amount);
			var totalGainLoss = holdings.Sum(h => h.GainLoss.Amount);
			var currency = holdings.FirstOrDefault()?.Currency ?? "?";
			var avgGainPct = holdings.Average(h => h.GainLossPercentage);

			var sb = new StringBuilder();
			sb.AppendLine("Portfolio Summary:");
			sb.AppendLine($"  Positions           : {holdings.Count}");
			sb.AppendLine($"  Total value         : {totalValue:N2} {currency}");
			sb.AppendLine($"  Total gain/loss     : {totalGainLoss:+N2;-N2} {currency}");
			sb.AppendLine($"  Avg gain/loss       : {avgGainPct:+N2;-N2}%");

			// Top 5 winners and losers — compact one-liner each
			var sorted = holdings.OrderByDescending(h => h.GainLossPercentage).ToList();

			sb.AppendLine("  Top 5 winners:");
			foreach (var h in sorted.Take(5))
			{
				sb.AppendLine($"    {h.Name,-22} {h.GainLossPercentage:+N1;-N1}%  {h.CurrentValue.Amount:N0} {currency}");
			}

			sb.AppendLine("  Top 5 losers:");
			foreach (var h in sorted.TakeLast(5).Reverse())
			{
				sb.AppendLine($"    {h.Name,-22} {h.GainLossPercentage:+N1;-N1}%  {h.CurrentValue.Amount:N0} {currency}");
			}

			return sb.ToString();
		}

		[Description("Returns upcoming dividend payments. Shows the next 10 by ex-date. Results include company, ex-date, payment date, expected amount and whether the dividend is predicted or confirmed.")]
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
			var upcoming = dividends.OrderBy(d => d.ExDate).Take(10).ToList();
			sb.AppendLine($"Next {upcoming.Count} upcoming dividends (of {dividends.Count} total):");

			foreach (var d in upcoming)
			{
				var predicted = d.IsPredicted ? "*" : " ";
				var amount = d.AmountPrimaryCurrency.HasValue
					? $"{d.AmountPrimaryCurrency.Value:N2} {d.PrimaryCurrency}"
					: $"{d.Amount:N2} {d.Currency}";
				sb.AppendLine($"  {predicted}{d.CompanyName,-20} ex:{d.ExDate:yyyy-MM-dd}  pay:{d.PaymentDate:yyyy-MM-dd}  {amount}  qty:{d.Quantity:N0}");
			}

			sb.AppendLine("  (* = predicted)");
			return sb.ToString();
		}

		[Description("Returns portfolio performance for a date range. Provides start/end value, % change, and quarterly snapshots. Dates must be yyyy-MM-dd. Defaults to the last 12 months.")]
		public async Task<string> GetPortfolioPerformance(
			[Description("Start date yyyy-MM-dd. Defaults to one year ago.")] string? startDate = null,
			[Description("End date yyyy-MM-dd. Defaults to today.")] string? endDate = null)
		{
			var start = ParseDateOrDefault(startDate, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)));
			var end = ParseDateOrDefault(endDate, DateOnly.FromDateTime(DateTime.UtcNow));

			await using var scope = scopeFactory.CreateAsyncScope();
			var service = scope.ServiceProvider.GetRequiredService<IHoldingsDataService>();
			var history = await service.GetPortfolioValueHistoryAsync(start, end, null);

			if (history.Count == 0)
			{
				return $"No portfolio history found between {start:yyyy-MM-dd} and {end:yyyy-MM-dd}.";
			}

			var first = history.First();
			var last = history.Last();
			var change = last.Value - first.Value;
			var changePct = first.Value != 0 ? change / first.Value * 100m : 0m;

			var sb = new StringBuilder();
			sb.AppendLine($"Performance {start:yyyy-MM-dd} → {end:yyyy-MM-dd}:");
			sb.AppendLine($"  Start : {first.Value:N2}  invested: {first.Invested:N2}");
			sb.AppendLine($"  End   : {last.Value:N2}  invested: {last.Invested:N2}");
			sb.AppendLine($"  Change: {change:+N2;-N2} ({changePct:+N2;-N2}%)");

			// Quarterly snapshots (last trading day of each quarter) — compact
			var quarters = history
				.GroupBy(p => new { p.Date.Year, Quarter = (p.Date.Month - 1) / 3 })
				.Select(g => g.Last())
				.OrderBy(p => p.Date)
				.ToList();

			if (quarters.Count > 0)
			{
				sb.AppendLine("  Quarterly snapshots:");
				foreach (var q in quarters)
				{
					sb.AppendLine($"    {q.Date:yyyy-MM-dd}  {q.Value:N0}");
				}
			}

			return sb.ToString();
		}

		private static string FormatHoldings(IList<GhostfolioSidekick.PortfolioViewer.WASM.Data.Models.HoldingDisplayModel> holdings, string header)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"{header}:");
			sb.AppendLine($"  {"Name",-22} {"Symbol",-8} {"Value",10} {"Qty",8} {"G/L%",7} {"Weight",7} {"Sector",-14}");

			foreach (var h in holdings)
			{
				var symbol = h.Symbols.FirstOrDefault() ?? "-";
				var glPct = $"{h.GainLossPercentage:+N1;-N1}%";
				var weight = $"{h.Weight:N1}%";
				sb.AppendLine($"  {h.Name,-22} {symbol,-8} {h.CurrentValue.Amount,10:N0} {h.Quantity,8:N2} {glPct,7} {weight,7} {h.Sector,-14}");
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
