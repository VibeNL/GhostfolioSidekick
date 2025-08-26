using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.Manual
{
	public class ManualSymbolRepository(
			DatabaseContext databaseContext,
			ICurrencyExchange currencyExchange) : ISymbolMatcher, IStockPriceRepository
	{
		public string DataSource => Datasource.MANUAL;

		public DateOnly MinDate => DateOnly.MinValue;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			foreach (var identifier in symbolIdentifiers)
			{
				var symbolProfileQuery = databaseContext.SymbolProfiles
						.Where(x => x.DataSource == Datasource.MANUAL);

				var symbolProfile = (await symbolProfileQuery
						.Where(x => identifier.AllowedAssetClasses == null || !identifier.AllowedAssetClasses!.Any() || identifier.AllowedAssetClasses!.Contains(x.AssetClass))
						.Where(x => identifier.AllowedAssetSubClasses == null || x.AssetSubClass == null || !identifier.AllowedAssetSubClasses!.Any() || identifier.AllowedAssetSubClasses!.Contains(x.AssetSubClass.Value))
						.ToListAsync()) // SQLlite does not support string operations that well
						.FirstOrDefault(x =>
							string.Equals(x.Symbol, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase) ||
							string.Equals(x.ISIN, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase) ||
							string.Equals(x.Name, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase) ||
							x.Identifiers.Any(y => string.Equals(y, identifier.Identifier, StringComparison.InvariantCultureIgnoreCase)));
				if (symbolProfile != null)
				{
					return symbolProfile;
				}
			}

			return null;
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			var list = await CalculatePricesBasedOnActivityUnitPrice(databaseContext, symbol);
			return list
				.Where(x => x.Date >= fromDate)
				.OrderBy(x => x.Date)
				.ToList();
		}


		private async Task<ICollection<MarketData>> CalculatePricesBasedOnActivityUnitPrice(
				DatabaseContext databaseContext,
				SymbolProfile profile)
		{
			var activities = await databaseContext.Activities
				.OfType<BuySellActivity>()
				.Where(x => x.Holding != null && x.Holding.SymbolProfiles.Contains(profile))
				.OrderBy(x => x.Date)
				.ToListAsync();

			if (activities.Count == 0)
			{
				return [];
			}

			var minDate = activities.Min(x => x.Date).Date;
			var list = new List<MarketData>();
			for (var date = minDate; date < DateTime.Today; date = date.AddDays(1))
			{
				var fromActivity = activities.Last(x => x.Date.Date <= date);
				var toActivity = activities.FirstOrDefault(x => x.Date.Date > date);
				var expectedPrice = await CalculateExpectedPrice(profile.Currency, fromActivity, toActivity, date);
				list.Add(new MarketData
				{
					Date = DateOnly.FromDateTime(date),
					Close = new Model.Money(fromActivity.UnitPrice!.Currency, expectedPrice),
					Open = new Model.Money(fromActivity.UnitPrice.Currency, expectedPrice),
					High = new Model.Money(fromActivity.UnitPrice.Currency, expectedPrice),
					Low = new Model.Money(fromActivity.UnitPrice.Currency, expectedPrice),
				});
			}

			return list;
		}

		private async Task<decimal> CalculateExpectedPrice(Model.Currency currency, BuySellActivity fromActivity, BuySellActivity? toActivity, DateTime date)
		{
			var a = (decimal)(date - fromActivity.Date).TotalDays;
			var b = (decimal)((toActivity?.Date ?? DateTime.Today.AddDays(1)) - date).TotalDays;

			var percentage = a / (a + b);
			decimal amountFrom = (await currencyExchange.ConvertMoney(fromActivity.UnitPrice, currency, DateOnly.FromDateTime(date))).Amount;
			decimal amountTo = (await currencyExchange.ConvertMoney(toActivity?.UnitPrice ?? fromActivity.UnitPrice ?? new Model.Money(currency, 0), currency, DateOnly.FromDateTime(date))).Amount;
			return amountFrom + percentage * (amountTo - amountFrom);
		}
	}
}
