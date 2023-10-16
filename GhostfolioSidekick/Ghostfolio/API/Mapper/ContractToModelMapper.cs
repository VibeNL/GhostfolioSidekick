using GhostfolioSidekick.Ghostfolio.API.Contract;
using GhostfolioSidekick.Model;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Ghostfolio.API.Mapper
{
	internal class ContractToModelMapper
	{
		public static Model.Account MapActivity(Contract.Account? rawAccount, RawActivity[] rawOrders, ConcurrentDictionary<string, Model.Asset> assets)
		{
			return new Model.Account(
				rawAccount.Id,
				rawAccount.Name,
				new Balance(new Money(CurrencyHelper.ParseCurrency(rawAccount.Currency), rawAccount.Balance, DateTime.MinValue)),
				rawOrders.Select(x =>
				{
					var asset = assets.GetOrAdd(x.SymbolProfile.Symbol, (y) => ParseSymbolProfile(x.SymbolProfile));
					return new Model.Activity(
										ParseType(x.Type),
										asset,
										x.Date,
										x.Quantity,
										new Money(asset.Currency, x.UnitPrice, x.Date),
										new Money(asset.Currency, x.Fee, x.Date),
										x.Comment,
										ParseReference(x.Comment)
										);
				}).ToList()
				);
		}

		public static Model.MarketDataInfo MapMarketDataInfo(Contract.MarketDataInfo marketDataInfo)
		{
			return new Model.MarketDataInfo
			{
				ActivitiesCount = marketDataInfo.ActivitiesCount,
				DataSource = marketDataInfo.DataSource,
				Symbol = marketDataInfo.Symbol,
			};
		}

		private static Model.Asset ParseSymbolProfile(Contract.SymbolProfile symbolProfile)
		{
			return new Model.Asset(
				CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.Name,
				symbolProfile.DataSource,
				symbolProfile.AssetSubClass,
				symbolProfile.AssetClass);
		}

		public static Model.Asset ParseSymbolProfile(Contract.Asset symbolProfile)
		{
			return new Model.Asset(
				CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.Name,
				symbolProfile.DataSource,
				symbolProfile.AssetSubClass,
				symbolProfile.AssetClass);
		}

		public static Model.MarketData MapMarketData(Market? market)
		{
			string? trackinsight = null;
			market.AssetProfile.SymbolMapping?.TryGetValue("TRACKINSIGHT", out trackinsight);
			return new Model.MarketData
			(
				market.AssetProfile.Symbol,
				market.AssetProfile.DataSource,
				market.AssetProfile.ActivitiesCount,
				trackinsight ?? string.Empty
			);
		}

		private static Model.ActivityType ParseType(Contract.ActivityType type)
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
	}
}
