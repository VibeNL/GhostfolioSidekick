using GhostfolioSidekick.Model;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Ghostfolio.API.Mapper
{
	internal static class ContractToModelMapper
	{
		public static Account MapAccount(Contract.Account rawAccount, Contract.Activity[] rawOrders)
		{
			var assets = new ConcurrentDictionary<string, Asset>();

			return new Account(
				rawAccount.Id,
				rawAccount.Name,
				new Balance(new Money(CurrencyHelper.ParseCurrency(rawAccount.Currency), rawAccount.Balance, DateTime.MinValue)),
				rawOrders.Select(x =>
				{
					return MapActivity(x, assets);
				}).ToList()
				);
		}

		public static Activity MapActivity(Contract.Activity x, ConcurrentDictionary<string, Asset> assets)
		{
			var asset = assets.GetOrAdd(x.SymbolProfile.Symbol, (y) => ParseSymbolProfile(x.SymbolProfile));
			return new Activity(
								ParseType(x.Type),
								asset,
								x.Date,
								x.Quantity,
								new Money(asset.Currency, x.UnitPrice, x.Date),
								new[] { new Money(asset.Currency, x.Fee, x.Date) },
								x.Comment,
								ParseReference(x.Comment)
								);
		}

		public static MarketData MapMarketData(Contract.MarketData marketData)
		{
			return new MarketData(marketData.Symbol, marketData.DataSource, marketData.MarketPrice, marketData.Date);
		}

		public static Asset ParseSymbolProfile(Contract.SymbolProfile symbolProfile)
		{
			return new Asset(
				CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.ISIN,
				symbolProfile.Name,
				symbolProfile.DataSource,
				Utilities.ParseEnum<AssetClass>(symbolProfile.AssetClass),
				Utilities.ParseEnum<AssetSubClass>(symbolProfile.AssetSubClass));
		}

		public static MarketDataList MapMarketDataList(Contract.MarketDataList? market)
		{
			string? trackinsight = null;
			market.AssetProfile.SymbolMapping?.TryGetValue("TRACKINSIGHT", out trackinsight);
			var mdl = new MarketDataList()
			{
				AssetProfile = new SymbolProfile
				{
					Currency = CurrencyHelper.ParseCurrency("EUR"),
					DataSource = market.AssetProfile.DataSource,
					Symbol = market.AssetProfile.Symbol,
					ActivitiesCount = market.AssetProfile.ActivitiesCount
				},
				MarketData = market.MarketData.Select(x => MapMarketData(x)).ToList()
			};
			mdl.AssetProfile.Mappings.TrackInsight = trackinsight;

			return mdl;
		}

		private static ActivityType ParseType(Contract.ActivityType type)
		{
			switch (type)
			{
				case Contract.ActivityType.BUY:
					return Model.ActivityType.Buy;
				case Contract.ActivityType.SELL:
					return Model.ActivityType.Sell;
				case Contract.ActivityType.DIVIDEND:
					return Model.ActivityType.Dividend;
				case Contract.ActivityType.INTEREST:
					return Model.ActivityType.Interest;
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}

		private static string ParseReference(string comment)
		{
			if (string.IsNullOrWhiteSpace(comment))
			{
				return null;
			}

			var pattern = @"Transaction Reference: \[(.*?)\]";
			var match = Regex.Match(comment, pattern);
			var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
			return key;
		}

		internal static Platform? MapAccount(Contract.Platform rawPlatform)
		{
			return new Platform(rawPlatform.Id, rawPlatform.Name, rawPlatform.Url);
		}
	}
}
