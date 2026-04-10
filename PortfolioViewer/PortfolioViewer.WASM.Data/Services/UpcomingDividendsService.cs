using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
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

			return entries.Select(e => new UpcomingDividendModel
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
			}).ToList();
		}

		private static async Task<Dictionary<string, decimal>> GetHoldingsDictionaryAsync(DatabaseContext db, DateOnly lastKnownDate)
		{
			var holdingsWithProfiles = await db.Holdings.Include(h => h.SymbolProfiles).ToListAsync();
			var snapshots = await db.CalculatedSnapshots.Where(s => s.Date == lastKnownDate).ToListAsync();
			var snapshotLookup = snapshots.GroupBy(s => s.HoldingId).ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));
			return holdingsWithProfiles
				.Select(h => new
				{
					Symbol = NormalizeSymbol(h.SymbolProfiles.FirstOrDefault()?.Symbol),
					Quantity = snapshotLookup.TryGetValue(h.Id, out var qty) ? qty : 0
				})
				.Where(x => !string.IsNullOrEmpty(x.Symbol) && x.Quantity > 0)
				.GroupBy(x => x.Symbol)
				.ToDictionary(g => g.Key!, g => g.Sum(x => x.Quantity));
		}

		private sealed class DividendWithProfile
		{
			public Dividend Dividend { get; set; } = default!;
			public SymbolProfile SymbolProfile { get; set; } = default!;
		}

		private static async Task<List<DividendWithProfile>> GetUpcomingDividendsWithProfilesAsync(DatabaseContext db)
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			return await db.Dividends
				.Where(dividend => dividend.PaymentDate >= today && dividend.Amount.Amount > 0)
				.Join(db.SymbolProfiles,
					dividend => new { Symbol = dividend.SymbolProfileSymbol, DataSource = dividend.SymbolProfileDataSource },
					symbolProfile => new { Symbol = (string?)symbolProfile.Symbol, DataSource = (string?)symbolProfile.DataSource },
					(dividend, symbolProfile) => new DividendWithProfile { Dividend = dividend, SymbolProfile = symbolProfile })
				.ToListAsync();
		}

		private static string NormalizeSymbol(string? symbol)
			=> symbol?.Trim().ToUpperInvariant() ?? string.Empty;

		private static bool TryGetHoldingQuantity(Dictionary<string, decimal> holdings, string? symbol, out decimal quantity)
			=> holdings.TryGetValue(NormalizeSymbol(symbol), out quantity);

       private static (decimal? dividendPerSharePrimary, decimal? expectedAmountPrimary, string primaryCurrencyLabel) ConvertDividendAmount(
			ICurrencyExchange currencyExchange,
			GhostfolioSidekick.Model.Money amount,
			GhostfolioSidekick.Model.Currency primaryCurrency,
			DateOnly exDividendDate,
			decimal dividendPerShare,
			decimal quantity,
			string nativeCurrency)
		{
			try
			{
				var converted = currencyExchange.ConvertMoney(amount, primaryCurrency, exDividendDate).Result;
				var dividendPerSharePrimary = converted.Amount;
				var expectedAmountPrimary = dividendPerSharePrimary * quantity;
				return (dividendPerSharePrimary, expectedAmountPrimary, primaryCurrency.Symbol);
			}
			catch
			{
				return (null, null, nativeCurrency);
			}
		}
	}
}
