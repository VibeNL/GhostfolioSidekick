//using GhostfolioSidekick.GhostfolioAPI.Contract;
//using GhostfolioSidekick.Model;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;

//namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
//{
//	internal static class ContractToModelMapper
//	{
//		public static Account MapAccount(Account rawAccount, Activity[] rawOrders)
//		{
//			var assets = new ConcurrentDictionary<string, SymbolProfile>();

//			return new Account(
//				rawAccount.Id,
//				rawAccount.Name,
//				new Balance(new Money(CurrencyHelper.ParseCurrency(rawAccount.Currency), rawAccount.Balance, DateTime.MinValue)),
//				rawAccount.Comment,
//				rawAccount.PlatformId,
//				rawOrders.Select(x =>
//				{
//					return MapActivity(x, assets);
//				}).ToList()
//				);
//		}

//		public static Activity MapActivity(Activity x, ConcurrentDictionary<string, SymbolProfile> assets)
//		{
//			var asset = assets.GetOrAdd(x.SymbolProfile!.Symbol!, (y) => ParseSymbolProfile(x.SymbolProfile));
//			return new Activity(
//								ParseType(x.Type),
//								asset,
//								x.Date,
//								x.Quantity,
//								new Money(asset.Currency, x.UnitPrice, x.Date),
//								new[] { new Money(asset.Currency, x.Fee, x.Date) },
//								x.Comment,
//								ParseReference(x.Comment)
//								);
//		}

//		public static MarketData MapMarketData(MarketData marketData)
//		{
//			return new MarketData(marketData.Symbol, marketData.DataSource, marketData.MarketPrice, marketData.Date);
//		}

//		public static SymbolProfile ParseSymbolProfile(SymbolProfile symbolProfile)
//		{
//			return new SymbolProfile(
//				CurrencyHelper.ParseCurrency(symbolProfile.Currency!),
//				symbolProfile.Symbol,
//				symbolProfile.ISIN,
//				symbolProfile.Name,
//				symbolProfile.DataSource,
//				Utilities.ParseEnum<AssetClass>(symbolProfile.AssetClass),
//				Utilities.ParseOptionalEnum<AssetSubClass>(symbolProfile.AssetSubClass),
//				symbolProfile.Comment);
//		}

//		public static MarketDataList MapMarketDataList(MarketDataList market)
//		{
//			string? trackinsight = null;
//			market.AssetProfile.SymbolMapping?.TryGetValue("TRACKINSIGHT", out trackinsight);
//			SymbolProfile assetProfile = market.AssetProfile;
//			var mdl = new MarketDataList()
//			{
//				AssetProfile = new SymbolProfile(
//					CurrencyHelper.ParseCurrency(assetProfile.Currency),
//					assetProfile.Symbol,
//					assetProfile.ISIN,
//					assetProfile.Name,
//					assetProfile.DataSource,
//					Utilities.ParseEnum<AssetClass>(assetProfile.AssetClass),
//					Utilities.ParseOptionalEnum<AssetSubClass>(assetProfile.AssetSubClass),
//					assetProfile.Comment)
//				{
//					ActivitiesCount = assetProfile.ActivitiesCount
//				},
//				MarketData = market.MarketData.Select(MapMarketData).ToList(),
//			};

//			mdl.AssetProfile.Mappings.TrackInsight = trackinsight;
//			return mdl;
//		}

//		private static ActivityType ParseType(ActivityType type)
//		{
//			switch (type)
//			{
//				case ActivityType.BUY:
//					return Model.ActivityType.Buy;
//				case ActivityType.SELL:
//					return Model.ActivityType.Sell;
//				case ActivityType.DIVIDEND:
//					return Model.ActivityType.Dividend;
//				case ActivityType.INTEREST:
//					return Model.ActivityType.Interest;
//				case ActivityType.FEE:
//					return Model.ActivityType.Fee;
//				default:
//					throw new NotSupportedException($"ActivityType {type} not supported");
//			}
//		}

//		private static string? ParseReference(string? comment)
//		{
//			if (string.IsNullOrWhiteSpace(comment))
//			{
//				return null;
//			}

//			var pattern = @"Transaction Reference: \[(.*?)\]";
//			var match = Regex.Match(comment, pattern);
//			var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
//			return key;
//		}

//		internal static Platform? MapAccount(Platform rawPlatform)
//		{
//			return new Platform(rawPlatform.Id, rawPlatform.Name, rawPlatform.Url);
//		}
//	}
//}
