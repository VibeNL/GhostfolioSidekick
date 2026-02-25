using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class UpcomingDividendsService(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		ICurrencyExchange currencyExchange,
		IServerConfigurationService serverConfigurationService) : IUpcomingDividendsService
	{
		public async Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
		{
			await using var databaseContext = await dbContextFactory.CreateDbContextAsync();

			// Get the primary currency to convert all amounts to
			var primaryCurrency = await serverConfigurationService.GetPrimaryCurrencyAsync();

			// Get the latest date for calculated snapshots
			var lastKnownDate = await databaseContext.CalculatedSnapshots
				.MaxAsync(x => (DateOnly?)x.Date);

			if (lastKnownDate == null)
			{
				return [];
			}

			// Fetch all holdings and their symbol profiles
			var holdingsWithProfiles = await databaseContext.Holdings
				.Include(h => h.SymbolProfiles)
				.ToListAsync();

			// Fetch all calculated snapshots for the latest date
			var snapshots = await databaseContext.CalculatedSnapshots
				.Where(s => s.Date == lastKnownDate)
				.ToListAsync();

			// Build holdings dictionary: symbol -> total quantity
			var holdingsDict = new Dictionary<string, decimal>();
			foreach (var holding in holdingsWithProfiles)
			{
				var quantity = snapshots
					.Where(s => s.HoldingId == holding.Id)
					.Sum(s => s.Quantity);

				var sp = holding.SymbolProfiles.FirstOrDefault();
				var symbol = sp?.Symbol;
				if (!string.IsNullOrEmpty(symbol) && quantity > 0)
				{
					if (holdingsDict.ContainsKey(symbol))
						holdingsDict[symbol] += quantity;
					else
						holdingsDict[symbol] = quantity;
				}
			}

			// Get upcoming dividends, join with SymbolProfiles using explicit properties
			var today = DateOnly.FromDateTime(DateTime.Today);
			var dividends = await databaseContext.Dividends
				.Where(dividend => dividend.PaymentDate >= today)
				.Join(databaseContext.SymbolProfiles,
					dividend => new { Symbol = dividend.SymbolProfileSymbol, DataSource = dividend.SymbolProfileDataSource },
					symbolProfile => new { Symbol = (string?)symbolProfile.Symbol, DataSource = (string?)symbolProfile.DataSource },
					(dividend, symbolProfile) => new { Dividend = dividend, SymbolProfile = symbolProfile })
				.Where(x => x.Dividend.Amount.Amount > 0)
				.ToListAsync();

			var result = new List<UpcomingDividendModel>();
			foreach (var item in dividends)
			{
				var symbol = item.SymbolProfile.Symbol ?? string.Empty;
				var companyName = item.SymbolProfile.Name ?? string.Empty;
				holdingsDict.TryGetValue(symbol, out var quantity);

				if (quantity <= 0)
				{
					continue;
				}

				// Native currency values (original dividend currency)
				var dividendPerShare = item.Dividend.Amount.Amount;
				var expectedAmount = dividendPerShare * quantity;
				var nativeCurrency = item.Dividend.Amount.Currency.Symbol;

				// Convert dividend per share to primary currency
				var dividendPerShareConverted = await currencyExchange.ConvertMoney(
					item.Dividend.Amount,
					primaryCurrency,
					item.Dividend.ExDividendDate);

				var dividendPerSharePrimaryCurrency = dividendPerShareConverted.Amount;
				var expectedAmountPrimaryCurrency = dividendPerSharePrimaryCurrency * quantity;

				result.Add(new UpcomingDividendModel
				{
					Symbol = symbol,
					CompanyName = companyName,
					ExDate = item.Dividend.ExDividendDate.ToDateTime(TimeOnly.MinValue),
					PaymentDate = item.Dividend.PaymentDate.ToDateTime(TimeOnly.MinValue),

					// Native currency (original dividend currency)
					Amount = expectedAmount,
					Currency = nativeCurrency,
					DividendPerShare = dividendPerShare,

					// Primary currency equivalent
					AmountPrimaryCurrency = expectedAmountPrimaryCurrency,
					PrimaryCurrency = primaryCurrency.Symbol,
					DividendPerSharePrimaryCurrency = dividendPerSharePrimaryCurrency,

					Quantity = quantity,
					IsPredicted = item.Dividend.DividendState == DividendState.Predicted
				});
			}

			return result;
		}
	}
}