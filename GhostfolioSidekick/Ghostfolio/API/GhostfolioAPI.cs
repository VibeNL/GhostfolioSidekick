using GhostfolioSidekick.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public class GhostfolioAPI : IGhostfolioAPI
	{
		private readonly Mapper mapper;
		private ILogger<GhostfolioAPI> logger;

		string url = Environment.GetEnvironmentVariable("GHOSTFOLIO_URL");
		string accessToken = Environment.GetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN");
		private RestCall restCall;

		public GhostfolioAPI(IMemoryCache memoryCache, ILogger<GhostfolioAPI> logger)
		{
			mapper = new Mapper();
			this.logger = logger;

			if (url != null && url.EndsWith("/"))
			{
				url = url.Substring(0, url.Length - 1);
			}

			restCall = new RestCall(memoryCache, logger, url, accessToken);
		}

		public async Task<Model.Account?> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Long());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				return null;
			}

			content = await restCall.DoRestGet($"api/v1/order?accounts={rawAccount.Id}", CacheDuration.None());
			var rawOrders = JsonConvert.DeserializeObject<RawActivityList>(content).Activities;

			var assets = new ConcurrentDictionary<string, Model.Asset>();

			return new Model.Account(
				rawAccount.Id,
				rawAccount.Name,
				new Money(CurrencyHelper.ParseCurrency(rawAccount.Currency), rawAccount.Balance),
				rawOrders.Select(x =>
				{
					var asset = assets.GetOrAdd(x.SymbolProfile.Symbol, (y) => ParseSymbolProfile(x.SymbolProfile));
					return new Model.Activity(
										ParseType(x.Type),
										asset,
										x.Date,
										x.Quantity,
										new Money(asset.Currency, x.UnitPrice),
										new Money(asset.Currency, x.Fee),
										x.Comment,
										ParseReference(x.Comment)
										);
				}).ToList()
				);
		}

		public async Task UpdateAccount(IEnumerable<Model.Account> accounts)
		{
			throw new NotImplementedException();
			//var ordersByAccount = DateTimeCollisionFixer.Fix(orders).GroupBy(x => x.AccountId);

			//foreach (var group in ordersByAccount)
			//{
			//	var mapped = group.Select(x => ConvertToNativeCurrency(x).Result).ToList();
			//	var existingOrders = await GetExistingOrders(group.First().AccountId);

			//	foreach (var mergeOrder in MergeOrders(mapped, existingOrders))
			//	{
			//		try
			//		{
			//			switch (mergeOrder.Operation)
			//			{
			//				case Operation.New:
			//					await WriteOrder(mergeOrder.Order1);
			//					break;
			//				case Operation.Duplicate:
			//					// Nothing to do!
			//					break;
			//				case Operation.Updated:
			//					await DeleteOrder(mergeOrder.Order2);
			//					await WriteOrder(mergeOrder.Order1);
			//					break;
			//				case Operation.Removed:
			//					await DeleteOrder(mergeOrder.Order2);
			//					break;
			//				default:
			//					break;
			//			}
			//		}
			//		catch (Exception ex)
			//		{
			//			logger.LogError($"Transaction failed to write {ex}, skipping");
			//		}
			//	}
			//}
		}

		public async Task<Money?> GetMarketPrice(Model.Asset asset, DateTime date)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{asset.DataSource}/{asset.Symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Market>(content);

			var marketData = market.MarketData.FirstOrDefault(x => x.Date == date.Date);

			if (marketData == null)
			{
				return null;
			}

			return new Money(asset.Currency, marketData.MarketPrice);
		}

		public async Task<Model.Asset?> FindSymbolByISIN(string? identifier, Func<IEnumerable<Model.Asset>, Model.Asset?> selector)
		{
			identifier = mapper.MapIdentifier(identifier);

			var content = await restCall.DoRestGet($"api/v1/symbol/lookup?query={identifier.Trim()}", CacheDuration.Long());
			var rawAssetList = JsonConvert.DeserializeObject<AssetList>(content);

			var assets = rawAssetList.Items.Select(x => ParseSymbolProfile(x));

			if (selector == null)
			{
				return LogIfEmpty(assets.FirstOrDefault(), identifier);
			}

			return LogIfEmpty(selector(assets), identifier);
		}

		public async Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date)
		{
			if (money.Currency == targetCurrency)
			{
				return money;
			}

			var sourceCurrency = mapper.MapCurrency(money.Currency.ToString());

			var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency}-{targetCurrency}/{date:yyyy-MM-dd}", CacheDuration.Short());

			dynamic stuff = JsonConvert.DeserializeObject(content);
			var token = stuff.marketPrice.ToString();
			var rate = (decimal)decimal.Parse(token);

			if (rate == 1)
			{
				return money;
			}

			return new Money(targetCurrency, rate * money.Amount);
		}

		private async Task WriteOrder(Activity order)
		{
			if (order.UnitPrice == 0 && order.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
			}

			var r = await restCall.DoRestPost($"api/v1/import", await ConvertToBody(order));
			bool emptyResponse = false;
			if (!r.IsSuccessStatusCode || (emptyResponse = r.Content.Equals("{\"activities\":[]}")))
			{
				var isduplicate = emptyResponse || (r.Content?.Contains("activities.1 is a duplicate activity") ?? false);
				if (isduplicate)
				{
					logger.LogDebug($"Duplicate transaction {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
					return;
				}

				throw new NotSupportedException($"Insert Failed {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
			}

			logger.LogInformation($"Added transaction {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
		}

		private async Task DeleteOrder(RawActivity? order)
		{
			var r = await restCall.DoRestDelete($"api/v1/order/{order.Id}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {order.Id}");
			}

			logger.LogInformation($"Deleted transaction {order.Id} {order.SymbolProfile.Symbol} {order.Date}");
		}

		private async Task<Model.Activity> ConvertToNativeCurrency(Model.Account account, Model.Activity order)
		{
			/*decimal Round(decimal value)
			{
				return Math.Round(value, 10);
			};

			decimal currencyConvertionRate = await GetExchangeRate(order.Currency, order.Asset.Currency, order.Date);
			decimal feeConvertionRate = order.Fee > 0 ? (await GetExchangeRate(order.FeeCurrency, order.Asset.Currency, order.Date)) : 1;

			var conversionComment = currencyConvertionRate != 1 ? $" | Original Price {order.UnitPrice}{order.Currency}, Fee {order.Fee}{order.Currency}" : string.Empty;
			return new Activity
			{
				AccountId = order.AccountId,
				Currency = order.Asset.Currency.ToString(),
				Asset = order.Asset,
				Comment = order.Comment + conversionComment,
				Date = order.Date,
				Fee = Round((await GetConvertedPrice(order.Fee, order.Asset.Currency, order.Date)).Amount),
				Quantity = Round(order.Quantity),
				Type = order.Type,
				UnitPrice = Round((await GetConvertedPrice(order.UnitPrice, order.Asset.Currency, order.Date)).Amount),
				ReferenceCode = order.ReferenceCode
			};*/
			return null;
		}

		private async Task<string> ConvertToBody(Activity order)
		{
			var o = new JObject();
			var r = new JObject
			{
				["activities"] = new JArray()
				{
					o
				}
			};

			o["accountId"] = order.AccountId;
			o["comment"] = order.Comment;
			o["currency"] = order.Currency;
			o["dataSource"] = order.Asset.DataSource;
			o["date"] = order.Date.ToString("o");
			o["fee"] = order.Fee;
			o["quantity"] = order.Quantity;
			o["symbol"] = order.Asset.Symbol;
			o["type"] = order.Type.ToString();
			o["unitPrice"] = order.UnitPrice;
			var res = r.ToString();
			return res;
		}

		private IEnumerable<MergeOrder> MergeOrders(IEnumerable<Model.Activity> ordersFromFiles, IEnumerable<RawActivity> existingOrders)
		{
			return null;
			//var pattern = @"Transaction Reference: \[(.*?)\]";

			//return ordersFromFiles.GroupJoin(existingOrders,
			//	fo => fo.ReferenceCode,
			//	eo =>
			//	{
			//		var match = Regex.Match(eo.Comment, pattern);
			//		var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
			//		return key;
			//	},
			//	(fo, eo) =>
			//	{
			//		if (fo != null && eo != null && eo.Any())
			//		{
			//			if (AreEquals(fo, eo.Single()))
			//			{
			//				return new MergeOrder(Operation.Duplicate, fo);
			//			}

			//			return new MergeOrder(Operation.Updated, fo, eo.Single());
			//		}
			//		else if (fo != null)
			//		{
			//			return new MergeOrder(Operation.New, fo);
			//		}
			//		else
			//		{
			//			return new MergeOrder(Operation.Removed, null, eo.Single());
			//		}
			//	});
		}

		private bool AreEquals(Activity fo, RawActivity eo)
		{
			return
				fo.Asset?.Symbol == eo.SymbolProfile?.Symbol &&
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
		}

		private sealed class MergeOrder
		{
			public MergeOrder(Operation operation, Activity order1)
			{
				Operation = operation;
				Order1 = order1;
				Order2 = null;
			}

			public MergeOrder(Operation operation, Activity order1, RawActivity? order2) : this(operation, order1)
			{
				Order2 = order2;
			}

			public Operation Operation { get; }

			public Activity Order1 { get; }

			public RawActivity? Order2 { get; }
		}

		private Model.Asset? LogIfEmpty(Model.Asset? asset, string identifier)
		{
			if (asset == null)
			{
				logger.LogError($"Could not find {identifier} as a symbol");
			}

			return asset;
		}

		private static Model.Asset ParseSymbolProfile(SymbolProfile symbolProfile)
		{
			return new Model.Asset(
CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.Name,
				symbolProfile.DataSource,
				symbolProfile.AssetSubClass,
				symbolProfile.AssetClass);
		}

		private static Model.Asset ParseSymbolProfile(Asset symbolProfile)
		{
			return new Model.Asset(
CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.Name,
				symbolProfile.DataSource,
				symbolProfile.AssetSubClass,
				symbolProfile.AssetClass);
		}

		private static string ParseReference(string comment)
		{
			var pattern = @"Transaction Reference: \[(.*?)\]";
			var match = Regex.Match(comment, pattern);
			var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
			return key;
		}

		private static Model.ActivityType ParseType(ActivityType type)
		{
			switch (type)
			{
				case ActivityType.BUY:
					return Model.ActivityType.Buy;
				case ActivityType.SELL:
					return Model.ActivityType.Sell;
				case ActivityType.DIVIDEND:
					return Model.ActivityType.Dividend;
				case ActivityType.INTEREST:
					return Model.ActivityType.Interest;
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}
	}
}
