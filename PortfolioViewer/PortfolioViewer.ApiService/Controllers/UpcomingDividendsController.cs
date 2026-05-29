using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UpcomingDividendsController(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IApplicationSettings applicationSettings) : ControllerBase
	{
		private string PrimaryCurrencySymbol =>
			applicationSettings.ConfigurationInstance?.Settings?.PrimaryCurrency ?? "EUR";

		[HttpGet]
		public async Task<IActionResult> GetUpcomingDividends(CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			var entries = await db.UpcomingDividendTimelineEntries
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			if (entries.Count == 0)
			{
				return Ok(Array.Empty<UpcomingDividendDto>());
			}

			var holdingIds = entries.Select(e => e.HoldingId).Distinct().ToList();

			var holdingData = await db.Holdings
				.AsNoTracking()
				.Where(h => holdingIds.Contains(h.Id))
				.Select(h => new
				{
					h.Id,
					Symbol = h.SymbolProfiles.Select(p => p.Symbol).FirstOrDefault(),
					Name = h.SymbolProfiles.Select(p => p.Name).FirstOrDefault(),
					LatestQuantity = h.CalculatedSnapshots
						.OrderByDescending(s => s.Date)
						.Select(s => s.Quantity)
						.FirstOrDefault()
				})
				.ToDictionaryAsync(h => h.Id, cancellationToken);

			var primaryCurrency = PrimaryCurrencySymbol;

			var result = entries.Select(entry =>
			{
				holdingData.TryGetValue(entry.HoldingId, out var holding);
				var quantity = holding?.LatestQuantity ?? 0m;
				return new UpcomingDividendDto
				{
					Symbol = holding?.Symbol ?? entry.HoldingId.ToString(),
					CompanyName = holding?.Name ?? string.Empty,
					ExDate = entry.ExDate,
					PaymentDate = entry.ExpectedDate,
					Amount = entry.Amount,
					Currency = entry.Currency?.Symbol ?? string.Empty,
					DividendPerShare = quantity > 0 ? entry.Amount / quantity : 0,
					AmountPrimaryCurrency = entry.AmountPrimaryCurrency,
					PrimaryCurrency = primaryCurrency,
					DividendPerSharePrimaryCurrency = quantity > 0 && entry.AmountPrimaryCurrency > 0 ? entry.AmountPrimaryCurrency / quantity : null,
					Quantity = quantity,
					IsPredicted = entry.DividendState == DividendState.Predicted
				};
			}).ToList();

			return Ok(result);
		}

		public class UpcomingDividendDto
		{
			public string Symbol { get; set; } = string.Empty;
			public string CompanyName { get; set; } = string.Empty;
			public DateOnly ExDate { get; set; }
			public DateOnly PaymentDate { get; set; }
			public decimal Amount { get; set; }
			public string Currency { get; set; } = string.Empty;
			public decimal DividendPerShare { get; set; }
			public decimal? AmountPrimaryCurrency { get; set; }
			public string PrimaryCurrency { get; set; } = string.Empty;
			public decimal? DividendPerSharePrimaryCurrency { get; set; }
			public decimal Quantity { get; set; }
			public bool IsPredicted { get; set; }
		}
	}
}
