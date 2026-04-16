using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// Service for retrieving upcoming dividends for portfolio holdings.
	/// </summary>
	public class UpcomingDividendsService(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IServerConfigurationService serverConfigurationService) : IUpcomingDividendsService
	{
		/// <summary>
		/// Retrieves upcoming dividends for all portfolio holdings.
		/// </summary>
		public async Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
		{
			await using var db = await dbContextFactory.CreateDbContextAsync();
			var primaryCurrency = await serverConfigurationService.GetPrimaryCurrencyAsync();

			var entries = await db.UpcomingDividendTimelineEntries
				.AsNoTracking()
				.ToListAsync();

			if (entries.Count == 0)
			{
				return [];
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
				.ToDictionaryAsync(h => h.Id);

			return entries.Select(entry =>
			{
				holdingData.TryGetValue(entry.HoldingId, out var holding);
				var quantity = holding?.LatestQuantity ?? 0m;
				return new UpcomingDividendModel
				{
					Symbol = holding?.Symbol ?? entry.HoldingId.ToString(),
					CompanyName = holding?.Name ?? string.Empty,
					ExDate = entry.ExDate,
					PaymentDate = entry.ExpectedDate,
					Amount = entry.Amount,
					Currency = entry.Currency?.Symbol ?? string.Empty,
					DividendPerShare = quantity > 0 ? entry.Amount / quantity : 0,
					AmountPrimaryCurrency = entry.AmountPrimaryCurrency,
					PrimaryCurrency = primaryCurrency.Symbol,
					DividendPerSharePrimaryCurrency = quantity > 0 && entry.AmountPrimaryCurrency > 0 ? entry.AmountPrimaryCurrency / quantity : null,
					Quantity = quantity,
					IsPredicted = entry.DividendState == DividendState.Predicted
				};
			}).ToList();
		}
	}
}
