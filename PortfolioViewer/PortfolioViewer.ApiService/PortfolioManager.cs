using GhostfolioSidekick.Database;
using PortfolioViewer.Model;

namespace PortfolioViewer.ApiService
{
	public class PortfolioManager
	{
		internal static Portfolio LoadPorfolio(DatabaseContext databaseContext) => new()
		{
			Accounts = [.. databaseContext.Accounts.Select(Map)],
			Activities = [.. databaseContext.Activities.Select(Map)],
			Holdings = [.. databaseContext.Holdings.Select(Map)],
			SymbolProfiles = [.. databaseContext.SymbolProfiles.Select(Map)],
		};

		private static SymbolProfile Map(GhostfolioSidekick.Model.Symbols.SymbolProfile symbolProfile)
		{
			return new SymbolProfile
			{
				Symbol = symbolProfile.Symbol,
				DataSource = symbolProfile.DataSource,
				Currency = Map(symbolProfile.Currency),
				AssetClass = symbolProfile.AssetClass.ToString(),
				AssetSubClass = symbolProfile.AssetSubClass.ToString(),
				StockSplits = [.. symbolProfile.StockSplits.Select(Map)],
				MarketData = [.. symbolProfile.MarketData.Select(Map)],
			};
		}

		private static MarketData Map(GhostfolioSidekick.Model.Market.MarketData marketData)
		{
			return new MarketData
			{
				Date = marketData.Date,
				Close = Map(marketData.Close),
			};
		}

		private static StockSplit Map(GhostfolioSidekick.Model.Market.StockSplit stockSplit)
		{
			return new StockSplit
			{
				Date = stockSplit.Date,
				BeforeSplit = stockSplit.BeforeSplit,
				AfterSplit = stockSplit.AfterSplit,
			};
		}

		private static Activity Map(GhostfolioSidekick.Model.Activities.Activity activity)
		{
			return new Activity
			{
				Id = activity.Id,
				Type = activity.GetType().Name,
				AccountId = activity.Account.Id,
				HoldingId = activity.Holding?.Id,
				Date = activity.Date,
				TransactionId = activity.TransactionId,
				Description = activity.Description,
				// TODO, Add other properties
			};
		}

		private static Holding Map(GhostfolioSidekick.Model.Holding holding)
		{
			return new Holding
			{
				Id = holding.Id,
				Symbols = [.. holding.SymbolProfiles.Select(MapId)]
			};
		}

		private static SymbolProfileId MapId(SymbolProfile profile)
		{
			return new SymbolProfileId
			{
				Symbol = profile.Symbol,
				Datasource = profile.DataSource
			};
		}

		private static Account Map(GhostfolioSidekick.Model.Accounts.Account account)
		{
			return new Account
			{
				Id = account.Id,
				Name = account.Name,
				Comment = account.Comment,
				Platform = Map(account.Platform),
				Balance = account.Balance.Select(Map).ToList(),
			};
		}

		private static Platform? Map(GhostfolioSidekick.Model.Accounts.Platform? platform)
		{
			if (platform == null)
			{
				return null;
			}

			return new Platform
			{
				Id = platform.Id,
				Name = platform.Name,
				Url	= platform.Url,
			};
		}

		private static Balance Map(GhostfolioSidekick.Model.Accounts.Balance balance)
		{
			return new Balance
			{
				Money = Map(balance.Money),
				Date = balance.Date,
			};
		}

		private static Model.Money Map(GhostfolioSidekick.Model.Money money)
		{
			return new Money { Currency = Map(money.Currency) };
		}

		private static Currency Map(GhostfolioSidekick.Model.Currency currency)
		{
			return new Currency { Symbol = currency.Symbol };
		}
	}
}
