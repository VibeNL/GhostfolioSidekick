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

			var data = await db.UpcomingDividendTimelineEntries
				.AsNoTracking()
				.GroupJoin(
					db.Holdings.Include(h => h.SymbolProfiles).Include(h => h.CalculatedSnapshots).AsNoTracking(),
					entry => entry.HoldingId,
					holding => holding.Id,
					(entry, holdings) => new { entry, holding = holdings.FirstOrDefault() }
				)
				.ToListAsync();

			var result = new List<UpcomingDividendModel>();
			foreach (var item in data)
			{
				var entry = item.entry;
				var holding = item.holding;
				var profile = (holding != null && holding.SymbolProfiles != null && holding.SymbolProfiles.Count > 0)
					? holding.SymbolProfiles.FirstOrDefault()
					: null;
				decimal quantity = 0;
				if (holding != null && holding.CalculatedSnapshots != null && holding.CalculatedSnapshots.Count > 0)
				{
					var latest = holding.CalculatedSnapshots.OrderByDescending(s => s.Date).FirstOrDefault();
					if (latest != null)
						quantity = latest.Quantity;
				}
				result.Add(new UpcomingDividendModel
				{
					Symbol = (profile != null && profile.Symbol != null) ? profile.Symbol : entry.HoldingId.ToString(),
					CompanyName = (profile != null && profile.Name != null) ? profile.Name : string.Empty,
					ExDate = entry.ExDate,
					PaymentDate = entry.ExpectedDate,
					Amount = entry.Amount,
					Currency = (entry.Currency != null && entry.Currency.Symbol != null) ? entry.Currency.Symbol : string.Empty,
					DividendPerShare = (quantity > 0) ? entry.Amount / quantity : 0,
					AmountPrimaryCurrency = entry.AmountPrimaryCurrency,
					PrimaryCurrency = primaryCurrency.Symbol,
					DividendPerSharePrimaryCurrency = (quantity > 0 && entry.AmountPrimaryCurrency > 0) ? entry.AmountPrimaryCurrency / quantity : null,
					Quantity = quantity,
					IsPredicted = entry.DividendState == DividendState.Predicted
				});
			}
			return result;
		}
	}
}
