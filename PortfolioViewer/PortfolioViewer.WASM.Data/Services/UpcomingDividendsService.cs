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
		ICurrencyExchange currencyExchange,
		IServerConfigurationService serverConfigurationService) : IUpcomingDividendsService
	{
		/// <summary>
		/// Retrieves upcoming dividends for all portfolio holdings.
		/// </summary>
		public async Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
		{
			await using var db = await dbContextFactory.CreateDbContextAsync();
			var primaryCurrency = await serverConfigurationService.GetPrimaryCurrencyAsync();
			var lastKnownDate = await db.CalculatedSnapshots.MaxAsync(x => (DateOnly?)x.Date);
			if (lastKnownDate is null) return [];

			var holdings = await GetHoldingsDictionaryAsync(db, lastKnownDate.Value);
			var dividends = await GetUpcomingDividendsWithProfilesAsync(db);

			return dividends
				.Where(d => TryGetHoldingQuantity(holdings, d.SymbolProfile.Symbol, out var qty) && qty > 0)
				.Select(d =>
				{
					var symbol = d.SymbolProfile.Symbol ?? string.Empty;
					var companyName = d.SymbolProfile.Name ?? string.Empty;
					var quantity = holdings[NormalizeSymbol(symbol)];
					var dividendPerShare = d.Dividend.Amount.Amount;
					var nativeCurrency = d.Dividend.Amount.Currency.Symbol;
					var (dividendPerSharePrimary, expectedAmountPrimary, primaryCurrencyLabel) = ConvertDividendAmount(
						currencyExchange, d.Dividend.Amount, primaryCurrency, d.Dividend.ExDividendDate, dividendPerShare, quantity, nativeCurrency
					);

					return new UpcomingDividendModel
					{
						Symbol = symbol,
						CompanyName = companyName,
						ExDate = DateTime.SpecifyKind(d.Dividend.ExDividendDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
						PaymentDate = DateTime.SpecifyKind(d.Dividend.PaymentDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
						Amount = dividendPerShare * quantity,
						Currency = nativeCurrency,
						DividendPerShare = dividendPerShare,
						AmountPrimaryCurrency = expectedAmountPrimary,
						PrimaryCurrency = primaryCurrencyLabel,
						DividendPerSharePrimaryCurrency = dividendPerSharePrimary,
						Quantity = quantity,
						IsPredicted = d.Dividend.DividendState == DividendState.Predicted
					};
				})
				.ToList();
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
