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
			var entries = await db.UpcomingDividendTimelineEntries.AsNoTracking().ToListAsync();

			return [.. entries.Select(e => new UpcomingDividendModel
			{
				Symbol = e.HoldingId.ToString(), // Optionally resolve symbol/profile here
				CompanyName = string.Empty, // Not available directly
				ExDate = DateTime.MinValue, // Not available
				PaymentDate = new DateTime(e.ExpectedDate.Year, e.ExpectedDate.Month, e.ExpectedDate.Day, 0, 0, 0, DateTimeKind.Utc),
				Amount = e.Amount,
				Currency = e.Currency?.Symbol ?? string.Empty,
				DividendPerShare = 0, // Not available
				AmountPrimaryCurrency = e.AmountPrimaryCurrency,
				PrimaryCurrency = primaryCurrency.Symbol,
				DividendPerSharePrimaryCurrency = null,
				Quantity = 0, // Not available
				IsPredicted = e.DividendState == DividendState.Predicted
			})];
		}
	}
}
