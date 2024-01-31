using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	internal static class ContractToModelMapper
	{
		public static Platform MapPlatform(Contract.Platform rawPlatform)
		{
			return new Platform(rawPlatform.Name)
			{
				Id = rawPlatform.Id,
				Url = rawPlatform.Url,
			};
		}

		public static Account MapAccount(Contract.Account rawAccount, Platform? platform)
		{
			return new Account(
				rawAccount.Name,
				new Balance(new Money(new Currency(rawAccount.Currency), rawAccount.Balance)))
			{
				Id = rawAccount.Id,
				Comment = rawAccount.Comment,
				Platform = platform,
			};
		}

		public static SymbolProfile ParseSymbolProfile(Contract.SymbolProfile symbolProfile)
		{
			return new SymbolProfile(
				symbolProfile.Symbol,
				symbolProfile.Name,
				new Currency(symbolProfile.Currency!),
				Utilities.ParseEnum<Datasource>(symbolProfile.DataSource),
				Utilities.ParseEnum<AssetClass>(symbolProfile.AssetClass),
				Utilities.ParseOptionalEnum<AssetSubClass>(symbolProfile.AssetSubClass))
			{
				Comment = symbolProfile.Comment,
				ISIN = symbolProfile.ISIN,
			};
		}

		public static MarketDataProfile MapMarketDataList(Contract.MarketDataList market)
		{
			string? trackinsight = null;
			market.AssetProfile.SymbolMapping?.TryGetValue("TRACKINSIGHT", out trackinsight);
			var assetProfile = market.AssetProfile;
			var mdl = new MarketDataProfile()
			{
				AssetProfile = MapSymbolProfile(assetProfile),
				MarketData = market.MarketData.Select(MapMarketData).ToList(),
			};

			mdl.AssetProfile.Mappings.TrackInsight = trackinsight;
			return mdl;
		}

		private static SymbolProfile MapSymbolProfile(Contract.SymbolProfile assetProfile)
		{
			return new SymbolProfile(
								assetProfile.Symbol,
								assetProfile.Name,
								new Currency(assetProfile.Currency),
								Utilities.ParseEnum<Datasource>(assetProfile.DataSource),
								Utilities.ParseEnum<AssetClass>(assetProfile.AssetClass),
								Utilities.ParseOptionalEnum<AssetSubClass>(assetProfile.AssetSubClass))
			{
				ActivitiesCount = assetProfile.ActivitiesCount,
				ISIN = assetProfile.ISIN,
				Comment = assetProfile.Comment,
				ScraperConfiguration = new ScraperConfiguration
				{
					Locale = assetProfile?.ScraperConfiguration?.Locale,
					Url = assetProfile?.ScraperConfiguration?.Url,
					Selector = assetProfile?.ScraperConfiguration?.Selector
				}
			};
		}

		public static MarketData MapMarketData(Contract.MarketData marketData)
		{
			return new MarketData(marketData.Symbol, Utilities.ParseEnum<Datasource>(marketData.DataSource), marketData.MarketPrice, marketData.Date);
		}

		internal static IEnumerable<Holding> MapToHoldings(Contract.Activity[] existingActivities)
		{
			var dict = new List<Holding>();

			foreach (var activity in existingActivities)
			{
				var profile = activity.SymbolProfile != null ? MapSymbolProfile(activity.SymbolProfile!) : null;
				var holding = dict.SingleOrDefault(x => (profile == null && x.SymbolProfile == null) || (x.SymbolProfile?.Equals(profile) ?? false));
				if (holding == null)
				{
					holding = new Holding(profile);
					dict.Add(holding);
				}

				holding.Activities.Add(MapActivity(activity));
			}

			return dict;
		}

		private static Activity MapActivity(Contract.Activity activity)
		{
			return new Activity(null!,
								ParseType(activity.Type),
								activity.Date,
								activity.Quantity,
								new Money(new Currency(activity.Currency!), activity.UnitPrice),
								null
								)
			{
				Fees = new[] { new Money(new Currency(activity.Currency!), activity.Fee) }
			};
		}

		private static ActivityType ParseType(Contract.ActivityType type)
		{
			switch (type)
			{
				case Contract.ActivityType.BUY:
					return ActivityType.Buy;
				case Contract.ActivityType.SELL:
					return ActivityType.Sell;
				case Contract.ActivityType.DIVIDEND:
					return ActivityType.Dividend;
				case Contract.ActivityType.INTEREST:
					return ActivityType.Interest;
				case Contract.ActivityType.FEE:
					return ActivityType.Fee;
				case Contract.ActivityType.ITEM:
					return ActivityType.Valuable;
				case Contract.ActivityType.LIABILITY:
					return ActivityType.Liability;
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}
	}
}
