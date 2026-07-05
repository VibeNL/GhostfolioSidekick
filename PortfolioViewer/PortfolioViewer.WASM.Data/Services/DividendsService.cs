using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// Service for retrieving dividends for portfolio holdings.
	/// </summary>
	public class DividendsService(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IServerConfigurationService serverConfigurationService) : IDividendsService
	{
		/// <summary>
		/// Retrieves dividends for all portfolio holdings, optionally filtered by date range.
		/// </summary>
		public async Task<List<DividendModel>> GetDividendsAsync(DateOnly? startDate = null, DateOnly? endDate = null)
		{
			await using var db = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
			var primaryCurrency = await serverConfigurationService.GetPrimaryCurrencyAsync();

			var upcomingResults = await GetUpcomingDividendsAsync(db, primaryCurrency, startDate, endDate);
			var activityResults = await GetActivityDividendsAsync(db, primaryCurrency, startDate, endDate);

			return [.. upcomingResults, .. activityResults];
		}

		private static async Task<List<DividendModel>> GetUpcomingDividendsAsync(
			DatabaseContext db,
			Model.Currency primaryCurrency,
			DateOnly? startDate,
			DateOnly? endDate)
		{
			var query = db.UpcomingDividendTimelineEntries.AsNoTracking();

			if (startDate.HasValue)
			{
				query = query.Where(e => e.ExpectedDate >= startDate.Value);
			}

			if (endDate.HasValue && endDate.Value < DateOnly.FromDateTime(DateTime.Today))
			{
				query = query.Where(e => e.ExpectedDate <= endDate.Value);
			}

			var entries = await query.ToListAsync();

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
				return new DividendModel
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

		private static async Task<List<DividendModel>> GetActivityDividendsAsync(
			DatabaseContext db,
			Model.Currency primaryCurrency,
			DateOnly? startDate,
			DateOnly? endDate)
		{
			var query = db.Activities
				.AsNoTracking()
				.OfType<DividendActivity>()
				.Where(a => a.Holding != null);

			if (startDate.HasValue)
			{
				var startDateTime = startDate.Value.ToDateTime(TimeOnly.MinValue);
				query = query.Where(a => a.Date >= startDateTime);
			}

			if (endDate.HasValue)
			{
				var endDateTime = endDate.Value.ToDateTime(TimeOnly.MaxValue);
				query = query.Where(a => a.Date <= endDateTime);
			}

			// Single query: project everything we need — no Includes, no snapshot joins
			var activities = await query
				.Select(a => new
				{
					a.Holding!.Id,
					Symbol = a.Holding.SymbolProfiles.Select(p => p.Symbol).FirstOrDefault(),
					Name = a.Holding.SymbolProfiles.Select(p => p.Name).FirstOrDefault(),
					ActivityDate = a.Date,
					Amount = a.Amount.Amount,
					CurrencySymbol = a.Amount.Currency != null ? a.Amount.Currency.Symbol : (string?)null,
					Quantity = a.Quantity
				})
				.ToListAsync();

			return activities.Select(activity =>
			{
				var amount = activity.Amount;
				var currency = activity.CurrencySymbol ?? string.Empty;
				var amountPrimary = activity.CurrencySymbol == primaryCurrency.Symbol ? amount : (decimal?)null;

				return new DividendModel
				{
					Symbol = activity.Symbol ?? activity.Id.ToString() ?? string.Empty,
					CompanyName = activity.Name ?? string.Empty,
					ExDate = DateOnly.FromDateTime(activity.ActivityDate),
					PaymentDate = DateOnly.FromDateTime(activity.ActivityDate),
					Amount = amount,
					Currency = currency,
					DividendPerShare = activity.Quantity > 0 ? amount / activity.Quantity : 0,
					AmountPrimaryCurrency = amountPrimary,
					PrimaryCurrency = primaryCurrency.Symbol,
					DividendPerSharePrimaryCurrency = activity.Quantity > 0 && amountPrimary > 0 ? amountPrimary / activity.Quantity : null,
					Quantity = activity.Quantity,
					IsPredicted = false
				};
			})
			.ToList();
		}
	}
}
