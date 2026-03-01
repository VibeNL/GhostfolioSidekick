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
		public async Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
		{
			await using var databaseContext = await dbContextFactory.CreateDbContextAsync();
			var primaryCurrency = await serverConfigurationService.GetPrimaryCurrencyAsync();
			var lastKnownDate = await databaseContext.CalculatedSnapshots
				.MaxAsync(x => (DateOnly?)x.Date);

			if (lastKnownDate == null)
			{
				return [];
			}

			var holdingsDict = await GetHoldingsDictionaryAsync(databaseContext, lastKnownDate.Value);
			var dividendsWithProfiles = await GetUpcomingDividendsWithProfilesAsync(databaseContext);

			var result = new List<UpcomingDividendModel>();
			foreach (var item in dividendsWithProfiles)
			{
				var symbol = item.SymbolProfile.Symbol ?? string.Empty;
				var companyName = item.SymbolProfile.Name ?? string.Empty;
				if (!holdingsDict.TryGetValue(symbol, out var quantity) || quantity <= 0)
				{
					continue;
				}

				var dividendPerShare = item.Dividend.Amount.Amount;
				var expectedAmount = dividendPerShare * quantity;
				var nativeCurrency = item.Dividend.Amount.Currency.Symbol;

				decimal dividendPerSharePrimaryCurrency = dividendPerShare;
				try
				{
					var dividendPerShareConverted = await currencyExchange.ConvertMoney(
						item.Dividend.Amount,
						primaryCurrency,
						item.Dividend.ExDividendDate);
					dividendPerSharePrimaryCurrency = dividendPerShareConverted.Amount;
				}
				catch
				{
					// Fallback to native amount if conversion fails
					dividendPerSharePrimaryCurrency = dividendPerShare;
				}

				var expectedAmountPrimaryCurrency = dividendPerSharePrimaryCurrency * quantity;

				result.Add(new UpcomingDividendModel
				{
					Symbol = symbol,
					CompanyName = companyName,
					ExDate = DateTime.SpecifyKind(item.Dividend.ExDividendDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
					PaymentDate = DateTime.SpecifyKind(item.Dividend.PaymentDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
					Amount = expectedAmount,
					Currency = nativeCurrency,
					DividendPerShare = dividendPerShare,
					AmountPrimaryCurrency = expectedAmountPrimaryCurrency,
					PrimaryCurrency = primaryCurrency.Symbol,
					DividendPerSharePrimaryCurrency = dividendPerSharePrimaryCurrency,
					Quantity = quantity,
					IsPredicted = item.Dividend.DividendState == DividendState.Predicted
				});
			}

			return result;
		}

		private static async Task<Dictionary<string, decimal>> GetHoldingsDictionaryAsync(DatabaseContext databaseContext, DateOnly lastKnownDate)
		{
			var holdingsWithProfiles = await databaseContext.Holdings
				.Include(h => h.SymbolProfiles)
				.ToListAsync();

			var snapshots = await databaseContext.CalculatedSnapshots
				.Where(s => s.Date == lastKnownDate)
				.ToListAsync();

			var snapshotLookup = snapshots
				.GroupBy(s => s.HoldingId)
				.ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));

			return holdingsWithProfiles
				.Select(h => new
				{
					Symbol = h.SymbolProfiles.FirstOrDefault()?.Symbol,
					Quantity = snapshotLookup.TryGetValue(h.Id, out var qty) ? qty : 0
				})
				.Where(x => !string.IsNullOrEmpty(x.Symbol) && x.Quantity > 0)
				.GroupBy(x => x.Symbol)
				.ToDictionary(g => g.Key!, g => g.Sum(x => x.Quantity));
		}

		private class DividendWithProfile
		{
			public Dividend Dividend { get; set; } = default!;
			public SymbolProfile SymbolProfile { get; set; } = default!;
		}

		private static async Task<List<DividendWithProfile>> GetUpcomingDividendsWithProfilesAsync(DatabaseContext databaseContext)
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			return await databaseContext.Dividends
				.Where(dividend => dividend.PaymentDate >= today)
				.Join(databaseContext.SymbolProfiles,
					dividend => new { Symbol = dividend.SymbolProfileSymbol, DataSource = dividend.SymbolProfileDataSource },
					symbolProfile => new { Symbol = (string?)symbolProfile.Symbol, DataSource = (string?)symbolProfile.DataSource },
					(dividend, symbolProfile) => new DividendWithProfile { Dividend = dividend, SymbolProfile = symbolProfile })
				.Where(x => x.Dividend.Amount.Amount > 0)
				.ToListAsync();
		}
	}
}
